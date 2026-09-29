Thank you for retesting. Your result shows that fixing the import did not resolve the web sign-in you reported: CredSSP off is rejected, and on brings up the wrong dialog.
We are keeping this open and will investigate the embedded RDP control's sign-in path against the working mstsc case you already supplied.
The current code requests Entra authentication but can silently ignore one unsupported-property error; that is a lead to check, not a confirmed explanation.
There is no verified new fix yet, and repeating the import or toggling CredSSP again would not add useful evidence.
