namespace Console

open System
open System.Configuration
open Microsoft.Owin.Hosting

module Program =
    

    [<EntryPoint>]
    let main argv =        
        let url = ConfigurationManager.AppSettings.["url"]
        use app = WebApp.Start<Startup>(url)     
        Console.WriteLine("Application started on {0}. ", url)
        Console.WriteLine("Press any key to terminate...")
        Console.ReadLine() |> ignore
        0 