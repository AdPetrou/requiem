import skyrim.requiem.build.ReleaseArchiveTask
import skyrim.requiem.build.BsaPackingTask
import skyrim.requiem.build.RequiemVersion
import java.io.BufferedReader
import java.io.InputStreamReader

plugins {
    base
}

if (JavaVersion.current() != JavaVersion.VERSION_13) {
    throw ProjectConfigurationException("Java 13 is required to build Requiem", listOf())
}

try {
    apply("userSetup.gradle")
} catch (ex: Exception) {
    logger.error("The config file 'userSetup.gradle' could not be found.")
    logger.quiet("This file specifies the settings specific to your setup.")
    logger.quiet("Please read the ReadMe file and follow its instructions to")
    logger.quiet("create your own config file with the appropriate paths.")
    throw ex
}

allprojects {
    version = RequiemVersion(4, 0, 1, "Threshold")
}

fun runCommand(command: List<String>): String = try {
    val process = ProcessBuilder(command).start()
    BufferedReader(InputStreamReader(process.inputStream)).readLine()
} catch (e: Exception) {
    "unknown"
}

val gitRevision by extra { runCommand(listOf("git", "rev-parse", "HEAD")) }
val gitBranch by extra { runCommand(listOf("git", "symbolic-ref", "--short", "-q", "HEAD")) }

val bsArch: File by extra

val skyProcDir = file("SkyProc Patchers")
val reqtificatorDir = file("$skyProcDir/Requiem")
val mutagenDir = file("Reqtificator")
val interfaceDir = file("Interface")
val scriptsDir = file("Scripts")
val sourceDir = file("Requiem/Source")
val scriptsSourcesDir = file(sourceDir.resolve("Scripts"))
val bsaFilesDir = file("BsaFiles")
val bsaFile = file("Requiem.bsa")

val copyReqtificator by tasks.registering(Copy::class) {
    dependsOn("components:reqtificator:assemble")
    val outputDir: File by project("components:reqtificator").extra
    from(outputDir)
    into(reqtificatorDir)
}

val copyMutagenReqtificator by tasks.registering(Copy::class) {
    dependsOn("components:mutagen-reqtificator:assemble")
    val outputDir: File by project("components:mutagen-reqtificator").extra
    from(outputDir)
    into(mutagenDir)
}

val copyScripts by tasks.registering(Copy::class) {
    dependsOn("components:papyrus-scripts:assemble")
    val outputDir: File by project("components:papyrus-scripts").extra
    from(outputDir)
    into(scriptsDir)
    exclude("**/*.psc")
}

val copyScriptSources by tasks.registering(Copy::class) {
    dependsOn("components:papyrus-scripts:assemble")
    val outputDir: File by project("components:papyrus-scripts").extra
    from(outputDir.resolve("source"))
    into(scriptsSourcesDir)
    exclude("**/*.pex")
}

val copyInterfaceFiles by tasks.registering(Copy::class) {
    dependsOn("components:interface:assemble")
    val outputDir: File by project("components:interface").extra
    from(outputDir)
    into(interfaceDir)
}

val copyBsaFiles by tasks.registering(Copy::class) {
    dependsOn("assemble")

    from(".")
    include("Interface/**", "meshes/**", "Sound/**", "textures/**", "Scripts/**")
    into(bsaFilesDir)
    exclude("**/REQ_Debug*.pex", "**/REQ_Debug*.psc")
}

val createBsa by tasks.registering(BsaPackingTask::class) {
    description = "create a BSA archive for Requiem's core assets"
    group = "distribution"
    dependsOn(copyBsaFiles)

    folder = bsaFilesDir
    archiveFile = bsaFile
    logFile = file("distribution/bsaLog.txt")
    archiveTool = bsArch
}

tasks.assemble {
    dependsOn(copyReqtificator)
    dependsOn(copyMutagenReqtificator)
    dependsOn(copyInterfaceFiles)
    dependsOn(copyScripts)
    dependsOn(copyScriptSources)
}

tasks.clean {
    delete(reqtificatorDir)
    delete(interfaceDir)
    delete(mutagenDir)
    delete(scriptsDir)
    delete(skyProcDir)
    delete(bsaFilesDir)
    delete(bsaFile)
}

val packRelease by tasks.registering(ReleaseArchiveTask::class) {
    description = "Pack Requiem as a ready to ship 7z archive"
    group = "distribution"

    dependsOn(tasks.assemble)
    dependsOn(createBsa)
    dependsOn("components:fomod-installer:assemble")
    dependsOn("components:documentation:assemble")

    archiveFile = file ("distribution/Requiem_${gitBranch}_$gitRevision.7z")

    val installerDir: File by project("components:fomod-installer").extra
    val releaseDocsDir: File by project("components:documentation").extra

    fomod = installerDir
    plugin = file("Requiem.esp")
    coreMod = files(
        "Reqtificator.bat",
        "Requiem.modgroups",
        releaseDocsDir,
        "Requiem.bsa",
        "Requiem",
        "BashTags"
    )
    excludePatterns = listOf()
}