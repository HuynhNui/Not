using System;
using System.Collections.Generic;

namespace _Project.Cutscenes
{
    [Serializable]
    public sealed class StoryCutsceneDefinition
    {
        public StoryCutsceneDefinition(string cutsceneId, IReadOnlyList<StoryDialogueLine> lines)
        {
            CutsceneId = cutsceneId;
            Lines = lines;
        }

        public string CutsceneId { get; }
        public IReadOnlyList<StoryDialogueLine> Lines { get; }
    }
}
