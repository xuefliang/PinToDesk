import SwiftUI

struct TodoRowView: View {
    @EnvironmentObject private var store: TodoStore
    let item: TodoItem
    let onEdit: () -> Void
    @State private var showActions = false

    var body: some View {
        HStack(spacing: 8) {
            // 完成按钮
            Button(action: {
                withAnimation(.easeInOut(duration: 0.2)) {
                    store.toggleComplete(item)
                }
            }) {
                Image(systemName: "square")
                    .font(.system(size: 16))
                    .foregroundColor(.secondary.opacity(0.7))
                    .frame(width: 28, height: 28)
            }
            .buttonStyle(.plain)

            // 圆点
            Circle()
                .fill(Color.secondary.opacity(0.35))
                .frame(width: 5, height: 5)
                .padding(.trailing, 4)

            // 标题
            Text(item.title)
                .font(.system(size: 15))
                .foregroundColor(.primary)
                .frame(maxWidth: .infinity, alignment: .leading)
                .lineLimit(10)

            // 编辑
            Button(action: onEdit) {
                Image(systemName: "pencil")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary.opacity(0.6))
                    .frame(width: 24, height: 24)
            }
            .buttonStyle(.plain)

            // 上移
            Button(action: { store.moveUp(item) }) {
                Image(systemName: "chevron.up")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary.opacity(0.6))
                    .frame(width: 24, height: 24)
            }
            .buttonStyle(.plain)

            // 下移
            Button(action: { store.moveDown(item) }) {
                Image(systemName: "chevron.down")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary.opacity(0.6))
                    .frame(width: 24, height: 24)
            }
            .buttonStyle(.plain)
        }
        .padding(.vertical, 7)
        .padding(.horizontal, 12)
        .background(
            RoundedRectangle(cornerRadius: 8, style: .continuous)
                .fill(Color.clear)
        )
        .contentShape(Rectangle())
        .onTapGesture(count: 2) {
            showActions = true
        }
    }
}
