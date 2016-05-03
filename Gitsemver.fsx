#I "scripts/packages/FAKE.Core/tools/"
#r "FakeLib.dll"

open Fake
open Fake.Git
open Fake.SemVerHelper
open System.IO

let defaultSemverFile = ".semver"

let getSemver repositoryDir semverFile =
    // get current version prefix from semver file
    let versionPrefix = System.IO.File.ReadLines semverFile |> Seq.tryHead
    if versionPrefix.IsNone then
        failwith "Unable to read semver prefix"

    // parse semver prefix
    tracefn "Semver file prefix is %s" versionPrefix.Value
    let semver = parse versionPrefix.Value    

    let semverCreatedSha = runSimpleGitCommand repositoryDir <| sprintf "log -G\"%s\" --reverse --max-count=1 --format=format:%%H -- %s" semverFile versionPrefix.Value
    let comitSinceCreated = runSimpleGitCommand repositoryDir <| sprintf "rev-list --no-merges --count %s..HEAD" semverCreatedSha

    let prereleaseInfo = 
        match semver.PreRelease with
        | Some ver ->     
            let buildCounterFixed = comitSinceCreated.PadLeft(3, '0')             
            let versionWithBuild = sprintf "%s-%s" ver.Origin buildCounterFixed           
            tracefn "Prerelease version: %A" versionWithBuild
            Some {
                PreRelease.Origin = versionWithBuild
                Name = versionWithBuild
                Number = None
            }
        | _ -> None

    { semver with 
        PreRelease = prereleaseInfo }

let getSemverDefault =
    let gitDirectory =
        try
            Some (findGitDir currentDirectory)
        with
            ex -> None
    
    if gitDirectory.IsNone then
        failwith "Unable to determine git directory"            

    getSemver gitDirectory.Value.FullName defaultSemverFile