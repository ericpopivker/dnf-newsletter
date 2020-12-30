using System;
using System.Threading.Tasks;

using PuppeteerSharp;
using HtmlAgilityPack;


//Download latest version of chrome if not already downloaded
Console.WriteLine("Checking if browser needs downloading. May take a minute if need to download.");
await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);

var launchOptions = new LaunchOptions
{
    //Headless means that Chrome UI will not be displayed
    //For debugging better keep it to false, but when it works, 
    //setting it to true can use a lot less resources
    Headless = false,
};

Browser browser = await Puppeteer.LaunchAsync(launchOptions);
var page = await browser.NewPageAsync();

string url = "https://google.com";
await page.GoToAsync(url);

//this is sometimes used to wait for page to finish rendering
//but in my experiences sometimes it waits forever
//await page.WaitForNavigationAsync();

//instead you can just use simple Task.Delay with seconds based on page 
//google is pretty fast
await Task.Delay(1000);

await page.TypeAsync("input[name='q']", ".NET Fiddle"); //enter what to search for

//await page.ClickAsync("input[name='btnK']"); //press google search button
//The code above doesn't actually work since there are 2 same buttons for running search
//luckily I can just execute JS to click the first button
string jsScript = "document.querySelector(\"input[name='btnK']\").click()";

await page.EvaluateExpressionAsync(jsScript);

//Wait for results to render
await Task.Delay(2000);

string htmlContent = await page.GetContentAsync();
ParseHtml(htmlContent);

Console.WriteLine("Press any key to close browser and program.");
Console.ReadKey();

await browser.CloseAsync();

static void ParseHtml(string htmlContent)
{
    //use HtmlAgilityPack
    var htmlDocument = new HtmlDocument();
    htmlDocument.LoadHtml(htmlContent);


    //parse google results using HtmlAgilityPack
    //
}
