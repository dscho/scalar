using NUnit.Framework;
using Scalar.FunctionalTests.Properties;
using Scalar.FunctionalTests.Should;
using Scalar.FunctionalTests.Tools;
using Scalar.Tests.Should;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Scalar.FunctionalTests.Tests.GitCommands
{
    [TestFixtureSource(typeof(GitRepoTests), nameof(GitRepoTests.ValidateWorkingTree))]
    [Category(Categories.GitCommands)]
    public class GitCommandsTests : GitRepoTests
    {
        public const string TopLevelFolderToCreate = "level1";
        private const string EncodingFileFolder = "FilenameEncoding";
        private const string EncodingFilename = "ريلٌأكتوبرûمارسأغسطسºٰٰۂْٗ۵ريلٌأك.txt";
        private const string ContentWhenEditingFile = "// Adding a comment to the file";
        private const string UnknownTestName = "Unknown";
        private const string SubFolderToCreate = "level2";

        private static readonly string EditFilePath = Path.Combine("GVFS", "GVFS.Common", "GVFSContext.cs");
        private static readonly string DeleteFilePath = Path.Combine("GVFS", "GVFS", "Program.cs");
        private static readonly string RenameFilePathFrom = Path.Combine("GVFS", "GVFS.Common", "Physical", "FileSystem", "FileProperties.cs");
        private static readonly string RenameFilePathTo = Path.Combine("GVFS", "GVFS.Common", "Physical", "FileSystem", "FileProperties2.cs");
        private static readonly string RenameFolderPathFrom = Path.Combine("GVFS", "GVFS.Common", "PrefetchPacks");
        private static readonly string RenameFolderPathTo = Path.Combine("GVFS", "GVFS.Common", "PrefetchPacksRenamed");

        public GitCommandsTests(Settings.ValidateWorkingTreeMode validateWorkingTree)
            : base(enlistmentPerTest: false, validateWorkingTree: validateWorkingTree)
        {
        }

        [TestCase]
        public void ChangeBranchAndMergeRebaseOnAnotherBranch()
        {
            this.ValidateGitCommand("checkout -b tests/functional/ChangeBranchAndMergeRebaseOnAnotherBranch_1");
            this.CreateFile();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand("add .");
            this.RunGitCommand("commit -m \"Create for ChangeBranchAndMergeRebaseOnAnotherBranch first branch\"");
            this.DeleteFile();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand("add .");
            this.RunGitCommand("commit -m \"Delete for ChangeBranchAndMergeRebaseOnAnotherBranch first branch\"");

            this.ValidateGitCommand("checkout " + this.ControlGitRepo.Commitish);
            this.ValidateGitCommand("checkout -b tests/functional/ChangeBranchAndMergeRebaseOnAnotherBranch_2");
            this.EditFile();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand("add .");
            this.RunGitCommand("commit -m \"Edit for ChangeBranchAndMergeRebaseOnAnotherBranch first branch\"");

            this.RunGitCommand("rebase --merge tests/functional/ChangeBranchAndMergeRebaseOnAnotherBranch_1", ignoreErrors: true);
            this.ValidateGitCommand("rev-parse HEAD^{{tree}}");
        }

        private void BasicCommit(Action fileSystemAction, string addCommand, [CallerMemberName]string test = GitCommandsTests.UnknownTestName)
        {
            this.ValidateGitCommand($"checkout -b tests/functional/{test}");
            fileSystemAction();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand(addCommand);
            this.RunGitCommand($"commit -m \"BasicCommit for {test}\"");
        }

        private void SwitchBranch(Action fileSystemAction, [CallerMemberName]string test = GitCommandsTests.UnknownTestName)
        {
            this.ValidateGitCommand("checkout -b tests/functional/{0}", test);
            fileSystemAction();
            this.ValidateGitCommand("status");
        }

        private void StageChangesSwitchBranch(Action fileSystemAction, [CallerMemberName]string test = GitCommandsTests.UnknownTestName)
        {
            this.ValidateGitCommand("checkout -b tests/functional/{0}", test);
            fileSystemAction();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand("add .");
        }

        private void CommitChangesSwitchBranch(Action fileSystemAction, [CallerMemberName]string test = GitCommandsTests.UnknownTestName)
        {
            this.ValidateGitCommand("checkout -b tests/functional/{0}", test);
            fileSystemAction();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand("add .");
            this.RunGitCommand("commit -m \"Change for {0}\"", test);
        }

        private void CommitChangesSwitchBranchSwitchBack(Action fileSystemAction, [CallerMemberName]string test = GitCommandsTests.UnknownTestName)
        {
            string branch = string.Format("tests/functional/{0}", test);
            this.ValidateGitCommand("checkout -b {0}", branch);
            fileSystemAction();
            this.ValidateGitCommand("status");
            this.ValidateGitCommand("add .");
            this.RunGitCommand("commit -m \"Change for {0}\"", branch);
            this.ValidateGitCommand("checkout " + this.ControlGitRepo.Commitish);
            this.Enlistment.RepoRoot.ShouldBeADirectory(this.FileSystem)
                .WithDeepStructure(this.FileSystem, this.ControlGitRepo.RootPath, withinPrefixes: this.pathPrefixes);

            this.ValidateGitCommand("checkout {0}", branch);
        }

        private void CreateFile()
        {
            this.CreateFile("Some content here", Path.GetRandomFileName() + "tempFile.txt");
            this.CreateFolder(TopLevelFolderToCreate);
            this.CreateFolder(Path.Combine(TopLevelFolderToCreate, SubFolderToCreate));
            this.CreateFile("File in new folder", Path.Combine(TopLevelFolderToCreate, SubFolderToCreate, Path.GetRandomFileName() + "folderFile.txt"));
        }

        private void EditFile()
        {
            this.AppendAllText(ContentWhenEditingFile, GitCommandsTests.EditFilePath);
        }

        private void DeleteFile()
        {
            this.DeleteFile(GitCommandsTests.DeleteFilePath);
        }

        private void RenameFile()
        {
            string virtualFileFrom = Path.Combine(this.Enlistment.RepoRoot, GitCommandsTests.RenameFilePathFrom);
            string virtualFileTo = Path.Combine(this.Enlistment.RepoRoot, GitCommandsTests.RenameFilePathTo);
            string controlFileFrom = Path.Combine(this.ControlGitRepo.RootPath, GitCommandsTests.RenameFilePathFrom);
            string controlFileTo = Path.Combine(this.ControlGitRepo.RootPath, GitCommandsTests.RenameFilePathTo);
            this.FileSystem.MoveFile(virtualFileFrom, virtualFileTo);
            this.FileSystem.MoveFile(controlFileFrom, controlFileTo);
            virtualFileFrom.ShouldNotExistOnDisk(this.FileSystem);
            controlFileFrom.ShouldNotExistOnDisk(this.FileSystem);
        }

        private void MoveFolder()
        {
            this.MoveFolder(GitCommandsTests.RenameFolderPathFrom, GitCommandsTests.RenameFolderPathTo);
        }
    }
}
