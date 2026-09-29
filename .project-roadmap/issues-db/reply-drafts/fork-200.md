We reproduced the same exception in the built application: save and reload a layout, then File → Open → Replace with another connection file.
The old Connections pane had been destroyed but was still receiving the file-loaded event, so it tried to rebuild a tree whose window handle no longer existed.
The local fix disconnects the disposed pane from those events. The full 7,323-test suite passes, and both normal File/Open and File/Open after layout reload now display the replacement tree without an exception in our isolated Windows lab.
No download containing this change has been published yet.
This establishes one reproducible path to the reported failure, not every possible trigger. If your steps did not involve loading a layout, please add that sequence.
