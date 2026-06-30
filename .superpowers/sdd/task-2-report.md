# Task 2 Report — TodoRowView: 增加编辑按钮

## What was implemented
- Added `onEdit: () -> Void` parameter to `TodoRowView`
- Added a pencil edit button (`Image(systemName: "pencil")`) between the title text and the up-arrow button in `TodoRowView`
- Updated `ContentView` to pass the `onEdit` callback to `TodoRowView`, setting `editingItem` and presenting `showEditSheet`

## Files changed
- `PinToDesk-iOS/Views/TodoRowView.swift` — added `onEdit` parameter and pencil button
- `PinToDesk-iOS/Views/ContentView.swift` — passed `onEdit` closure

## Issues or concerns
- None. The existing edit sheet (`EditTodoView`) is already wired in ContentView via `.sheet`, so the new button reuses the same flow.
