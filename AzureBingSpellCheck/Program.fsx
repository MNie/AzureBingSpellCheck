#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"
#r "../packages/Newtonsoft.Json.9.0.1/lib/net40/Newtonsoft.Json.dll"

open FSharp.Data
open Newtonsoft.Json
open System.IO
open FSharp.Data.HttpRequestHeaders
open System

type responseJson = 
    {
        _type: string
        flaggedTokens: flaggedToken list
    }
and flaggedToken = 
    {
        offset: int
        token: string
        ``type``: string
        suggestions: suggestion list
    }
and suggestion =
    {
        suggestion: string
        score: double
    }

let loadFile path =
    File.ReadAllText path

let saveJson (json, fileName: string) =
    use outFile = new StreamWriter(fileName)
    (
        outFile.Write(JsonConvert.SerializeObject json)
    )

let getSpellCheck text mode =
    let response = Http.RequestString(sprintf "https://api.cognitive.microsoft.com/bing/v5.0/spellcheck/?mode=%s" mode,
        body = TextRequest (sprintf "Text=%s" text),
        headers = [ContentType "application/x-www-form-urlencoded"; "Ocp-Apim-Subscription-Key", "{api-key}"])
    JsonConvert.DeserializeObject<responseJson>(response)

let getCorrectedText(text: string, response: responseJson) =
    let mutable returnText = text
    response.flaggedTokens
    |> Seq.iter (fun x -> 
            let bestSuggestion = x.suggestions |> Seq.sortBy (fun y -> y.score) |> Seq.head
            returnText <- returnText.Remove(x.offset, x.token.Length).Insert(x.offset, bestSuggestion.suggestion)
        )
    returnText

let run textPath mode correctedTextPath =
    let baseText = loadFile textPath
    getCorrectedText(baseText, (getSpellCheck baseText mode))
    |> fun x -> saveJson(x, correctedTextPath)

run @"{path of file with input text}" "spell" @"{path of file with output text}"
