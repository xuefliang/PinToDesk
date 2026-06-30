import Foundation

class TodoStore: ObservableObject {
    @Published var items: [TodoItem] = []

    private let storageKey = "PinToDesk_todos"

    init() {
        load()
    }

    // MARK: - CRUD

    func add(title: String) {
        let item = TodoItem(title: title)
        items.append(item)
        save()
    }

    func toggleComplete(_ item: TodoItem) {
        guard let idx = items.firstIndex(of: item) else { return }
        items[idx].isCompleted.toggle()
        items[idx].completedAt = items[idx].isCompleted ? Date() : nil
        save()
    }

    func update(_ item: TodoItem, title: String) {
        guard let idx = items.firstIndex(of: item) else { return }
        items[idx].title = title
        save()
    }

    func delete(_ item: TodoItem) {
        items.removeAll { $0.id == item.id }
        save()
    }

    func moveUp(_ item: TodoItem) {
        guard let idx = items.firstIndex(of: item), idx > 0 else { return }
        items.swapAt(idx, idx - 1)
        save()
    }

    func moveDown(_ item: TodoItem) {
        guard let idx = items.firstIndex(of: item), idx < items.count - 1 else { return }
        items.swapAt(idx, idx + 1)
        save()
    }

    var activeItems: [TodoItem] {
        items.filter { !$0.isCompleted }
    }

    var completedItems: [TodoItem] {
        items.filter { $0.isCompleted }.sorted { $0.completedAt ?? $0.createdAt < $1.completedAt ?? $1.createdAt }
    }

    // MARK: - Persistence

    private func save() {
        if let data = try? JSONEncoder().encode(items) {
            UserDefaults.standard.set(data, forKey: storageKey)
        }
    }

    private func load() {
        guard let data = UserDefaults.standard.data(forKey: storageKey),
              let decoded = try? JSONDecoder().decode([TodoItem].self, from: data) else { return }
        items = decoded
    }
}
