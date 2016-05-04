#nowarn "211"
#I "scripts/packages/FAKE.Core/tools/"
#I "../../../packages/FAKE.Lib/lib/net451/"
#r "FakeLib.dll"

open Fake
open Fake.Git
open Fake.SemVerHelper
open System.IO

let defaultSemverFile = ".semver"

/// Get semver info
/// ## Parameters
///
/// - 'repositoryDir' - git directory
/// - 'semverFile' - semver marker file (default .semver)
let getSemverInfo repositoryDir semverFile =
    // get current version prefix from semver file
    let versionPrefix = System.IO.File.ReadLines semverFile |> Seq.tryHead
    if versionPrefix.IsNone then
        failwithf "Unable to read semver base from '%s'" semverFile

    // parse semver prefix
    tracefn "Semver file prefix is: %s" versionPrefix.Value
    let semver = parse versionPrefix.Value    

    let semverCreatedSha = runSimpleGitCommand repositoryDir <| sprintf "log -G\"%s\" --reverse --max-count=1 --format=format:%%H -- %s" semverFile versionPrefix.Value    
    let commitCountExpr = 
        match isNullOrEmpty semverCreatedSha with
            | false -> sprintf "%s..HEAD" semverCreatedSha
            | true -> "HEAD"
    let comitsSinceCreated = runSimpleGitCommand repositoryDir <| sprintf "rev-list --no-merges --count %s" commitCountExpr

    (semver, comitsSinceCreated)

/// Get semver info using default values
let getSemverInfoDefault =
    let gitDirectory =
        try
            Some (findGitDir currentDirectory)
        with
            ex -> None
    
    if gitDirectory.IsNone then
        failwith "Unable to determine git directory"            

    getSemverInfo gitDirectory.Value.FullName defaultSemverFile

/// Append prerelease build number (based on getSemverInfo comitsSinceCreated) 
let appendPreReleaseBuildNumber fixedWidth (semver: SemVerInfo, comitsSinceCreated: string) =
    let prereleaseInfo = 
        match semver.PreRelease with
        | Some ver ->     
            let buildCounterFixed = comitsSinceCreated.PadLeft(fixedWidth, '0')             
            let versionWithBuild = sprintf "%s-%s" ver.Origin buildCounterFixed           
            tracefn "Appended semver prerelease number: %A" versionWithBuild
            Some {
                PreRelease.Origin = versionWithBuild
                Name = versionWithBuild
                Number = None
            }
        | _ -> None

    { semver with PreRelease = prereleaseInfo }  

    