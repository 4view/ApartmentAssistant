using System.Threading.Tasks;

namespace ApartmentAssisnant.Selenium;

public class SeleniumService
{
    private readonly ILogger<SeleniumService> _logger;

    private readonly ChromeDriver _driver;

    private readonly WebDriverWait _wait;

    public SeleniumService(ILogger<SeleniumService> logger)
    {
        _logger = logger;

        var options = new ChromeOptions();
        options.AddArguments(
            //"--headless", // Для работы без GUI
            "--no-sandbox", // Для Linux
            "--disable-dev-shm-usage",
            "--window-size=1920,1080"
        );

        _driver = new ChromeDriver(options);

        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
    }

    // /html/body/div/div[4]/div/div[1]/div - alertError (alert alert-error)
    /// /html/body/div/div[4]/div/div[1]/div - alertSucces (alert alert-success)
    /// <summary>
    /// Авторизация на сайте
    /// </summary>
    public bool Authorization(UserEntity user, string captchaText)
    {
        try
        {
            var loginInput = _driver.FindElement(By.Id("ls"));
            var passwordInput = _driver.FindElement(By.Id("pwd"));
            var captchaInput = _driver.FindElement(By.Id("vpb_captcha_code"));
            var enterButton = _driver.FindElement(By.Id("btn"));
            var captchaImg = _driver.FindElement(By.Id("captchaimg"));
            var captchaResponse = _driver.FindElement(By.Id("captchaResponse"));

            ClearInputFields(loginInput, passwordInput, captchaInput);

            loginInput.SendKeys(user.Login);
            passwordInput.SendKeys(user.Password);
            captchaInput.SendKeys(captchaText);

            enterButton.Click();

            if (IsElementExist(By.Id("captchaResponse")))
            {
                if (captchaResponse.Text == "Введенный Вами код неправильный. Попробуйте еще раз.")
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Передаем показания <paramref name="indications"/> счетчиков в поля для ввода
    /// </summary>
    public async Task<ActionResponse> InputTenementIndications(TenementIndicationEntity indications)
    {
        var link = _driver.FindElement(By.XPath("//*[@id=\"nav-collapse-subhead\"]/ul/li[4]/a"));
        link.Click();

        var submitButton = _driver.FindElement(
            By.XPath("//input[contains(@onclick, 'this.form.submit();')]")
        );

        var kitchenColdWaterInput = _driver.FindElement(
            By.XPath(
                "//table[@class=\"table table-bordered table-striped\"]/tbody/tr[1]/td[6]/input[1]"
            )
        );
        var kitchenHotWaterInput = _driver.FindElement(
            By.XPath(
                "//table[@class=\"table table-bordered table-striped\"]/tbody/tr[4]/td[6]/input[1]"
            )
        );
        var bathroomColdWaterInput = _driver.FindElement(
            By.XPath(
                "//table[@class=\"table table-bordered table-striped\"]/tbody/tr[2]/td[6]/input[1]"
            )
        );
        var bathroomHotWaterInput = _driver.FindElement(
            By.XPath(
                "//table[@class=\"table table-bordered table-striped\"]/tbody/tr[3]/td[6]/input[1]"
            )
        );

        kitchenColdWaterInput.SendKeys(indications.KitchenColdWater.ToString());
        kitchenHotWaterInput.SendKeys(indications.KitchenHotWater.ToString());
        bathroomColdWaterInput.SendKeys(indications.BathroomColdWater.ToString());
        bathroomHotWaterInput.SendKeys(indications.BathroomHotWater.ToString());
        await Task.Delay(1000);
        submitButton.Click();

        try
        {
            var alertError = _driver.FindElements(By.XPath("//div[@class=\"alert alert-error\"]"));

            var alertSucces = _driver.FindElements(
                By.XPath("//div[@class=\"alert alert-success\"]")
            );

            if (alertSucces.Count > 0)
            {
                var successText = alertSucces[0].Text;
                return new ActionResponse { SuccessResponse = $"{successText}" };
            }
            else if (alertError.Count > 0)
            {
                var errorText = alertError[0].Text;
                return new ActionResponse { ErrorResponse = $"{errorText}" };
            }
            else
            {
                return new ActionResponse
                {
                    ErrorResponse = "Не удалось определить результат операции!",
                };
            }
        }
        catch (Exception ex)
        {
            return new ActionResponse { ErrorResponse = $"Произошла ошибка: {ex.Message}" };
        }
    }

    /// <summary>
    /// Проверяет существует ли искомый элемент
    /// </summary>
    private bool IsElementExist(By by)
    {
        try
        {
            _driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <summary>
    /// Отчищает поля для ввода
    /// </summary>
    /// <param name="webElements"></param>
    private void ClearInputFields(params IWebElement[] webElements)
    {
        foreach (var item in webElements)
        {
            item.Clear();
        }
    }

    /// <summary>
    /// Метод для навигации на страницу авторизации
    /// </summary>
    private async Task NavigateToCaptchaPageAsync()
    {
        _driver.Navigate().GoToUrl("https://lk.ric-nv.ru/");

        await Task.Delay(1000);
    }

    /// <summary>
    /// Метод ожидания прогрузки элемента капчи на странице
    /// </summary>
    private async Task<IWebElement?> WaitForCaptchaElementAsync()
    {
        try
        {
            var element = _wait.Until(_driver =>
            {
                try
                {
                    var el = _driver.FindElement(By.Id("captchaimg"));
                    return el.Displayed ? el : null;
                }
                catch
                {
                    return null;
                }
            });

            await Task.Delay(500);

            return element;
        }
        catch
        {
            try
            {
                var elements = _driver.FindElements(By.ClassName("vpb_captcha_wrapper"));
                return elements.FirstOrDefault(e => e.Displayed);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Обновляет изображение капчи
    /// </summary>
    private async Task RefreshCaptchaAsync()
    {
        try
        {
            var refreshButton = _driver.FindElement(By.CssSelector("a.refresh-cap"));

            if (refreshButton != null && refreshButton.Displayed)
            {
                refreshButton.Click();

                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Не удалось обновить капчу {ex.Message}");
        }
    }

    /// <summary>
    /// Создает скриншот с изображением капчи
    /// </summary>
    /// <param name="captchaElement">Веб элемент капчи</param>
    private byte[] TakeElementScreenshot(IWebElement captchaElement)
    {
        var location = captchaElement.Location;
        var size = captchaElement.Size;

        var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

        using var image = Image.Load<Rgba32>(screenshot.AsByteArray);

        image.Mutate(ctx =>
            ctx.Crop(
                new SixLabors.ImageSharp.Rectangle(location.X, location.Y, size.Width, size.Height)
            )
        );

        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, new PngEncoder());

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Получает изображение капчи
    /// </summary>
    /// <returns>Массив байт с изображением</returns>
    public async Task<byte[]?> CaptureCaptchaAsStreamAsync(bool isLoaded)
    {
        try
        {
            if (!isLoaded)
            {
                await NavigateToCaptchaPageAsync();
            }

            var captchaElement = await WaitForCaptchaElementAsync();

            if (captchaElement == null)
            {
                captchaElement = await WaitForCaptchaElementAsync();
                await RefreshCaptchaAsync();
            }

            if (captchaElement == null)
                return null;

            var screenshotBytes = TakeElementScreenshot(captchaElement);
            return screenshotBytes;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Ошибка: {ex}");
            return null;
        }
    }
}
