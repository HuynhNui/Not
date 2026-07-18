namespace _Project.Editor.Voice
{
    internal sealed class VoiceManifestRecord
    {
        public string VoiceId { get; set; }
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public int LineIndex { get; set; }
        public string Speaker { get; set; }
        public string Emotion { get; set; }
        public string PsychologyPhase { get; set; }
        public string Tag { get; set; }
        public string Text { get; set; }
        public string OutputPath { get; set; }
        public string Status { get; set; }
        public string DuplicateOf { get; set; }
        public string Notes { get; set; }
    }
}
