using mRemoteNG.Tools;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Connection
{
    public enum ConnectionFrameColor
    {
        [LocalizedAttributes.LocalizedDescription(nameof(Language.FrameColorNone))]
        None = 0,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.FrameColorRed))]
        Red = 1,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.FrameColorYellow))]
        Yellow = 2,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.FrameColorGreen))]
        Green = 3,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.FrameColorBlue))]
        Blue = 4,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.FrameColorPurple))]
        Purple = 5
    }
}
