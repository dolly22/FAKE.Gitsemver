#I @"packages/FAKE.Core/tools/"
#r @"FakeLib.dll"

#load @"../gitsemver.fsx"

open Fake
open Fake.Git
open Fake.SemVerHelper
open Gitsemver

let mutable version : SemVerHelper.SemVerInfo option = None

Target "UpdateVersion" (fun _ ->  
    let semver = 
        getSemverInfoDefault 
        |> appendPreReleaseBuildNumber 3 

    version <- Some semver
)

Target "Default" <| DoNothing

"UpdateVersion"
    ==> "Default"

// start build
RunTargetOrDefault "Default"