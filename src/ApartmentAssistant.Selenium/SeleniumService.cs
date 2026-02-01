namespace ApartmentAssisnant.Selenium;

public class SeleniumService
{
    private readonly CapthcaSessionService _captchaService;

    private readonly ILogger<SeleniumService> _logger;

    private readonly ChromeDriver _driver;
    private readonly WebDriverWait _wait;

    public SeleniumService(CapthcaSessionService capthcaSession, ILogger<SeleniumService> logger)
    {
        _captchaService = capthcaSession;
        _logger = logger;

        var options = new ChromeOptions();
        options.AddArguments(
            // "--headless", // Для работы без GUI
            "--no-sandbox", // Для Linux
            "--disable-dev-shm-usage",
            "--window-size=1920,1080"
        );

        _driver = new ChromeDriver(options);

        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Метод для авторизации на сайте
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
                else
                {
                    _driver.Quit();
                    return true;
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
    /// <returns>Массив байтов</returns>
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
