///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

var artifactsDir = Directory("./artifacts");
var packages = "./artifacts/packages";
var solutionPath = "./Curiosity.SPSS.sln";
var framework = "netstandard2.0";

var nugetSource = "https://api.nuget.org/v3/index.json";
var nugetApiKey = Argument<string>("nugetApiKey", null);

Task("Clean")
    .Does(() => 
    {            
        DotNetCoreClean(solutionPath);        
        DirectoryPath[] cleanDirectories = new DirectoryPath[] {
            artifactsDir
        };
    
        CleanDirectories(cleanDirectories);
    
        foreach(var path in cleanDirectories) { EnsureDirectoryExists(path); }
    
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does(() => 
    {
        var settings = new DotNetCoreBuildSettings
          {
              Configuration = configuration
          };
          
        DotNetCoreBuild(
            solutionPath,
            settings);
    });

Task("UnitTests")
    .Does(() =>
    {        
        Information("UnitTests task...");
        var projects = GetFiles("./tests/UnitTests/**/*csproj");
        foreach(var project in projects)
        {
            Information(project);
            
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = false
                });
        }
    });
    
Task("Pack")
    .Does(() =>
    {        
         Information("Packing to nupkg...");
         var settings = new DotNetCorePackSettings
          {
              Configuration = configuration,
              OutputDirectory = packages
          };
         
          DotNetCorePack(solutionPath, settings);
    });
 
Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
    {
         var pushSettings = new DotNetCoreNuGetPushSettings
         {
             Source = nugetSource,
             ApiKey = nugetApiKey,
             SkipDuplicate = true
         };

         var pkgs = GetFiles($"{packages}/*.nupkg");
         foreach(var pkg in pkgs)
         {
             Information($"Publishing \"{pkg}\".");
             DotNetCoreNuGetPush(pkg.FullPath, pushSettings);
         }
 });
 
Task("ForcePublish")
    .IsDependentOn("Pack")
    .Does(() =>
    {
         var pushSettings = new DotNetCoreNuGetPushSettings 
         {
             Source = nugetSource,
             ApiKey = nugetApiKey,
             SkipDuplicate = true
         };
         
         var pkgs = GetFiles($"{packages}/*.nupkg");
         foreach(var pkg in pkgs) 
         {     
             Information($"Publishing \"{pkg}\".");
             DotNetCoreNuGetPush(pkg.FullPath, pushSettings);
         }
 }); 
    
Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests");
    
Task("GitHub")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish");
  
RunTarget(target);
