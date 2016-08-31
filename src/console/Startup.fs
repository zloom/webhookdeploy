﻿namespace Console

open global.Owin
open Microsoft.Owin

open Newtonsoft.Json

open System
open System.Collections.Generic
open System.Configuration
open System.Diagnostics
open System.IO
open System.Security
open System.Threading.Tasks
open System.Net

module Settings =    
    
    let user = ConfigurationManager.AppSettings.["user"]
    
    let password = 
        ConfigurationManager.AppSettings.["password"].ToCharArray() 
        |> Seq.fold (fun (s : SecureString) a -> s.AppendChar(a); s) (new SecureString())
    
    let handlers = 
        File.ReadAllText(ConfigurationManager.AppSettings.["handlers"])
        |> fun text -> JsonConvert.DeserializeObject<Dictionary<String, String>>(text)
        |> Dictionary
    

type ReceiverMiddleware(next : OwinMiddleware) = 
    inherit OwinMiddleware(next)

    let output (arg: DataReceivedEventArgs) = 
        if (isNull >> not) arg.Data 
        then sprintf "%O:%s" DateTime.UtcNow arg.Data
        else ""   

    let execute (context: IOwinContext) cmd = 
        let script = sprintf "/C \"%s\"" Settings.handlers.[context.Request.Path.Value]           
        let startInfo = ProcessStartInfo(FileName = "cmd.exe", Arguments = script)
            
        startInfo.UserName <- Settings.user
        startInfo.Password <- Settings.password
        startInfo.CreateNoWindow <- true
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        context.Response.StatusCode <- int HttpStatusCode.OK
        context.Response.ContentType <- "text/html"
        use proc = new Process(StartInfo = startInfo)
        proc.OutputDataReceived.Add(output >> (sprintf "<p>%s</p>") >> context.Response.Write)
        proc.ErrorDataReceived.Add(output >> (sprintf "<p>%s</p>") >> context.Response.Write)
        proc.OutputDataReceived.Add(output >> Console.WriteLine)
        proc.ErrorDataReceived.Add(output >> Console.WriteLine)                       
                            
        proc.Start() |> ignore
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        proc.WaitForExit()

    override __.Invoke(context) = 
        async {   
            match Settings.handlers.TryGetValue context.Request.Path.Value with
            | true, cmd -> do! async { execute context cmd } 
            | false, _  -> context.Response.StatusCode <- int HttpStatusCode.NotFound   
            return context       
        }
        |> Async.StartAsTask :> Task

type Startup() =    

    member __.Configuration(app:Owin.IAppBuilder) =           
        app.Use<ReceiverMiddleware>() |> ignore

        
        

