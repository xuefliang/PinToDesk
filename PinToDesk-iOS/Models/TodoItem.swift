import Foundation

struct TodoItem: Identifiable, Codable, Equatable {
    var id = UUID()
    var title: String
    var isCompleted = false
    var createdAt = Date()
    var completedAt: Date?
}
