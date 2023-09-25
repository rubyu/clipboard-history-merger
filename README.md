# Clipboard History Merger

Clipboard History Merger is a utility for managing a history of clipboard contents, and pasting a concatenation of recent clipboard entries using a keyboard shortcut.

## Features

- Monitors clipboard and maintains a history of the last 10 entries.
- Utilizes a FIFO (First In, First Out) mechanism for managing clipboard history.
- Monitors keyboard shortcut `Ctrl + Alt + V`. Upon a sequence of consecutive presses within a 1-second interval, concatenates corresponding number of recent clipboard entries and pastes the concatenated string into the foreground application.
- Utilizes Win32 `SendInput` function to send text to the foreground application.

## Requirements

- Windows operating system.
- .NET Framework 4.6.1 or later.

## Installation

### Build
1. Clone the repository: `git clone https://github.com/rubyu/clipboard-history-merger.git`.
2. Open `ClipboardHistoryMerger.sln` in Visual Studio.
3. Build the solution to produce the `ClipboardHistoryMerger.exe` executable.

### Download
Download a ZIP archive and extract it.

## Usage

1. Run `ClipboardHistoryMerger.exe`.
2. Copy text to clipboard as usual.
3. Press `Ctrl + Alt + V` one or more times to paste concatenated clipboard history into the foreground application.

## License

MIT License. See `LICENSE` file for details.

## License

MIT License

## Author

rubyu [![Github icon](http://i.imgur.com/9I6NRUm.png)](https://github.com/rubyu) [![Twitter icon](http://i.imgur.com/wWzX9uB.png)](https://twitter.com/ruby_u)
