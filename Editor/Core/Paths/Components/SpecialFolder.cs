using System;
using ThunderKit.Core.Pipelines;
using UnityEngine;

namespace ThunderKit.Core.Paths.Components
{
    public class SpecialFolder : PathComponent
    {
        [Tooltip("Resolved per platform. ApplicationData: %APPDATA%, ~/.config, ~/Library/Application Support")]
        public Environment.SpecialFolder Folder = Environment.SpecialFolder.ApplicationData;

        protected override string GetPathInternal(PathReference output, Pipeline pipeline)
        {
            string folderPath;
            try
            {
                folderPath = Environment.GetFolderPath(Folder);
            }
            catch (ArgumentException argumentException)
            {
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, this)} is set to {(int)Folder}, which is not a known special folder.",
                    argumentException);
            }

            if (string.IsNullOrEmpty(folderPath))
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, this)} requested {Folder}, which has no location on this platform.");

            return folderPath;
        }
    }
}
