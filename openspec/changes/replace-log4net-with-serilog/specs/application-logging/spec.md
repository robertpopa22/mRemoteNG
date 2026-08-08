## ADDED Requirements

### Requirement: File-based diagnostic logging
The system SHALL write diagnostic log messages (accepted via the `MessageCollector`/`IMessageWriter` pipeline, plus direct logger calls from options and command-line handling) to a rolling log file on disk, using Serilog as the logging engine.

#### Scenario: Message written to log file
- **WHEN** a message is accepted by the application's message/logging pipeline at Debug, Info, Warning, or Error severity
- **THEN** a corresponding line is appended to the active log file containing a timestamp, thread identifier, severity level, and the message text

#### Scenario: Exception logged with stack trace
- **WHEN** an exception is logged through the logging pipeline
- **THEN** the log file contains the exception's message and stack trace associated with the log entry

### Requirement: Size-based log rotation
The system SHALL roll the active log file over to a backup when it exceeds 10 MB, retaining at most 5 rolled-over backup files, with no time-based (daily) rotation.

#### Scenario: Log file exceeds size limit
- **WHEN** the active log file reaches 10 MB in size and a new message is logged
- **THEN** the system rolls the current file into a backup and continues logging new messages to a fresh active log file

#### Scenario: Backup file count exceeds retention limit
- **WHEN** a rotation would produce more than 5 retained backup log files
- **THEN** the oldest backup file is discarded so at most 5 backups are retained

### Requirement: Runtime log path reconfiguration
The system SHALL allow the active log file's directory/path to be changed at runtime, without requiring an application restart, and SHALL continue logging to the newly configured path immediately after the change.

#### Scenario: User changes log file location in Options
- **WHEN** the user changes the log file path/directory setting on the Options → Notifications page and the setting is applied
- **THEN** subsequent log messages are written to the new location and no messages are silently dropped during the switch

### Requirement: Level-based message filtering upstream of the log sink
The system SHALL continue to filter which message classes (Debug/Info/Warning/Error) are written to the log file according to the existing user-configurable notification filtering settings, independent of the underlying logging engine.

#### Scenario: Debug messages disabled by user setting
- **WHEN** the user has disabled writing Debug-class messages via the notification filtering settings
- **THEN** Debug-class messages are not written to the log file, while Info/Warning/Error messages continue to be written per their own enabled/disabled settings
