# Clipboard History Merger

Clipboard History Merger is a utility for recording the history of clipboard contents, and pasting **a concatenation** of recent clipboard entries using a keyboard shortcut.

## Features

- Monitors the clipboard and maintains a history of the last 10 entries.
- Monitors the keyboard shortcut `Ctrl + Alt + V`. Upon a sequence of consecutive presses within a 1-second interval, it concatenates the corresponding number of recent clipboard entries and pastes the resulting string into the foreground application.

## Examples of actual use cases

| Clipboard history (older - newer) | Activated shortcuts | What will be sent |
| ------------- | ------------- | ------------- |
| [The, quick, brown, fox, jumped, over, the, lazy, dogs.] | [Ctrl+Alt+V] | dogs. |
| [The, quick, brown, fox, jumped, over, the, lazy, dogs.] | [Ctrl+Alt+V] x2 | lazy dogs. |
| [The, quick, brown, fox, jumped, over, the, lazy, dogs.] | [Ctrl+Alt+V] x3 | the lazy dogs. |
| [The, quick, brown, fox, jumped, over, the, lazy, dogs.] | [Ctrl+Alt+V] x4 | over the lazy dogs. |

## Requirements

- Windows 7 or later.
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
2. Copy text to the clipboard as usual.
3. Press `Ctrl + Alt + V` one or more times to paste concatenated clipboard history into the foreground application.

## License

MIT License. See `LICENSE` file for details.

## Author

rubyu [![Github icon](http://i.imgur.com/9I6NRUm.png)](https://github.com/rubyu) [![Twitter icon](http://i.imgur.com/wWzX9uB.png)](https://twitter.com/ruby_u)
