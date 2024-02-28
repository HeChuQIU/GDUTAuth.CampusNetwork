open System
open System.Net.Http
open System.Net.NetworkInformation
open System.Threading
open Argu

type CliArguments =
    | [<Mandatory>] R of uri: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | R _ -> "登录请求 URI"

let errorHandler =
    ProcessExiter(
        colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red
    )

let parser = ArgumentParser.Create<CliArguments>(errorHandler = errorHandler)
let argv = Environment.GetCommandLineArgs() |> Array.skip 1
let argParseResults = parser.ParseCommandLine argv

let consoleWithColor color f =
    let originalColor = Console.ForegroundColor
    Console.ForegroundColor <- color
    f ()
    Console.ForegroundColor <- originalColor

let isWebsiteAccessible (url: string) =
    let ping = new Ping()
    let result = ping.Send(url)

    // 检查 ping 请求的结果
    match result.Status with
    | IPStatus.Success -> true
    | _ -> false

let testWebsiteUri = "www.bing.com"

let Authenticate (uri: Uri) =
    let client = new HttpClient()
    let response = client.GetAsync(uri) |> Async.AwaitTask |> Async.RunSynchronously
    response

let client = new HttpClient()

let rec main (retry: int) =
    let uri = argParseResults.GetResult R

    let success =
        try
            let response = Authenticate <| Uri uri
            isWebsiteAccessible testWebsiteUri
        with ex ->
            consoleWithColor ConsoleColor.Red (fun _ -> printfn $"发生错误: {ex.Message}")
            false

    match success, retry with
    | true, _ ->
        consoleWithColor ConsoleColor.Green (fun _ -> printfn "登录成功")
        0
    | false, _ when retry <= 0 ->
        consoleWithColor ConsoleColor.Red (fun _ -> printfn "登录失败，请检查网络环境请求 URI 是否正确")
        0
    | false, _ ->
        consoleWithColor ConsoleColor.Red (fun _ -> printfn $"登录失败，剩余 %d{retry} 次重试...")
        Thread.Sleep(TimeSpan.FromSeconds(2))
        main (retry - 1)

exit (main 4)
