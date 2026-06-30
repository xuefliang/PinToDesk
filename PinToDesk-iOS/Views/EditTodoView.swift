import SwiftUI

struct EditTodoView: View {
    @EnvironmentObject private var store: TodoStore
    @Environment(\.dismiss) private var dismiss
    let item: TodoItem
    @State private var text: String

    init(item: TodoItem) {
        self.item = item
        _text = State(initialValue: item.title)
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("待办内容", text: $text)
                        .font(.system(size: 16))
                }
            }
            .navigationTitle("编辑待办")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("取消") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("保存") {
                        let t = text.trimmingCharacters(in: .whitespaces)
                        if !t.isEmpty {
                            store.update(item, title: t)
                        }
                        dismiss()
                    }
                    .disabled(text.trimmingCharacters(in: .whitespaces).isEmpty)
                }
            }
        }
    }
}
