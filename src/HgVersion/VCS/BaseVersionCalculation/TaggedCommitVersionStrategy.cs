using Mercurial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCSVersion;
using VCSVersion.SemanticVersions;
using VCSVersion.VCS;
using VCSVersion.VersionCalculation.BaseVersionCalculation;

namespace HgVersion.VCS.BaseVersionCalculation
{
    /// <summary>
    /// Class which fully utilizes Mercurial.Net logging API capabilities in order to improve speed of
    /// base version calculation on huge (10000+) revisions repositories.
    /// </summary>
    class FastTaggedCommitVersionStrategy : IBaseVersionStrategy
    {
        public IEnumerable<BaseVersion> GetVersions(IVersionContext context)
        {
            Logger.WriteInfo($"Started base version calc by {nameof(FastTaggedCommitVersionStrategy)}");
            var ret = GetTaggedVersions(context);
            Logger.WriteInfo($"Ended base version calc by {nameof(FastTaggedCommitVersionStrategy)}");
            return ret;
        }

        private static IEnumerable<BaseVersion> GetTaggedVersions(IVersionContext context)
        {
            var repo = new Repository(context.Repository.Path);
            var currentCommit = context.CurrentCommit;
            var taggedAncestors = RevSpec
                .Tagged()
                .And(
                    RevSpec.AncestorsOf(new RevSpec(currentCommit.Hash))
                );
                                
            var taggedCommits = repo
                .Log(
                    new LogCommand().WithRevision(taggedAncestors)
                )
                .Select(c =>
                {
                    var versionAndTag = c.Tags.Select(tag =>
                    {
                        if (!SemanticVersion.TryParse(tag, context.Configuration.TagPrefix, out var version))
                            return null;
                        else
                            return new { Version = version, Tag = tag };
                    }).Where(v => v != null)
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault();

                    if (versionAndTag != null)
                        return new VersionTaggedCommit(new HgCommit(c), versionAndTag.Version, versionAndTag.Tag);
                    else
                        return null;
                });

            return taggedCommits.Where(c => c != null).Select(t => CreateBaseVersion(context, t));
        }

        private static BaseVersion CreateBaseVersion(IVersionContext context, VersionTaggedCommit version)
        {
            var shouldUpdateVersion = version.Commit.Hash != context.CurrentCommit.Hash;
            return new BaseVersion(FormatType(version), version.SemVer, version.Commit, shouldUpdateVersion);
        }

        private static string FormatType(VersionTaggedCommit version)
        {
            return $"Hg tag '{version.Tag}'";
        }

        private sealed class VersionTaggedCommit
        {
            public string Tag { get; }
            public ICommit Commit { get; }
            public SemanticVersion SemVer { get; }

            public VersionTaggedCommit(ICommit commit, SemanticVersion semVer, string tag)
            {
                Tag = tag;
                Commit = commit;
                SemVer = semVer;
            }
        }
    }
}
