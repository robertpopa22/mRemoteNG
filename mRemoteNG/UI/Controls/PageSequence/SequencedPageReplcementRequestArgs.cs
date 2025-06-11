using System;

namespace mRemoteNG.UI.Controls.PageSequence
{
    public delegate void SequencedPageReplcementRequestHandler(object sender, SequencedPageReplcementRequestArgs args);

    public enum RelativePagePosition
    {
        PreviousPage,
        CurrentPage,
        NextPage
    }

    public class SequencedPageReplcementRequestArgs(SequencedControl newControl, RelativePagePosition pageToReplace)
    {
        public SequencedControl NewControl { get; } = newControl ?? throw new ArgumentNullException(nameof(newControl));
        public RelativePagePosition PagePosition { get; } = pageToReplace;
    }
}