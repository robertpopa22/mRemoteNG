not a defect

Thank you for the batch file and the screenshot. They show why the quotes are visible. External Tools adds them when it constructs the command line for a `.cmd` or `.bat` file so a value containing spaces or commas can stay one argument. There is no setting to turn off that quoting.

With your three sample values, a Windows batch check printed `"arg1" "arg2" "arg3"` through `%*`, while `%~1`, `%~2` and `%~3` returned `arg1`, `arg2` and `arg3`. The [Windows batch parameter reference](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/call#batch-parameters) describes the `%~n` syntax. If a specific executable still rejects an argument, please reopen with its name and a sample using dummy values. [How this fork handles reports](https://github.com/robertpopa22/mRemoteNG/issues/167)

If this project helps you, please star it: https://github.com/robertpopa22/mRemoteNG
Qodo's free open-source plan requires at least 200 stars on this repository (or on another public repository in the same GitHub organization). Other tools we use ask for more. A star is what keeps those tools available.
