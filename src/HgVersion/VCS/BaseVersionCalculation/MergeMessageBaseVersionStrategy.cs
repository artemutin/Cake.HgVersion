using Mercurial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCSVersion;
using VCSVersion.SemanticVersions;
using VCSVersion.VCS;
using VCSVersion.VersionCalculation.BaseVersionCalculation;

namespace HgVersion.VCS.BaseVersionCalculation
{
    public class FastMergeMessageBaseVersionStrategy : IBaseVersionStrategy
    {
        public IEnumerable<BaseVersion> GetVersions(IVersionContext context)
        {

            Logger.WriteInfo($"Started base version calc by {nameof(FastMergeMessageBaseVersionStrategy)}");
            var repo = new Repository(context.Repository.Path);
            var baseVersions = repo.Log(
                new LogCommand()
                .WithRevision
                    (
                        RevSpec.Merges.And(
                        RevSpec.AncestorsOf(new RevSpec(context.CurrentCommit.Hash))
                        )
                    )
                ).SelectMany(c =>
                {
                    var commit = new HgCommit(c);

                    if (TryParse(commit, context, out var semanticVersion))
                    {
                        var shouldIncrement = !context.Configuration.PreventIncrementForMergedBranchVersion;
                        var baseVersion = new BaseVersion(FormatType(commit), semanticVersion, commit, shouldIncrement);

                        return new[] { baseVersion };
                    }

                    return Enumerable.Empty<BaseVersion>();
                });

            Logger.WriteInfo($"Ended base version calc by {nameof(FastMergeMessageBaseVersionStrategy)}");

            return baseVersions;
        }

        private static string FormatType(ICommit commit)
        {
            return $"Merge message '{commit.CommitMessage?.Trim()}'";
        }

        private static bool TryParse(ICommit mergeCommit, IVersionContext context, out SemanticVersion semanticVersion)
        {
            semanticVersion = Inner(mergeCommit, context);
            return semanticVersion != null;
        }

        private static SemanticVersion Inner(ICommit mergeCommit, IVersionContext context)
        {
            var mergeMessage = context
                .RepositoryMetadataProvider
                .ParseMergeMessage(mergeCommit.CommitMessage);

            return mergeMessage.Version;
        }
    }
}
