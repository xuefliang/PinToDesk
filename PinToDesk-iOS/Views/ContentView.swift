import SwiftUI
import UniformTypeIdentifiers

struct ContentView: View {
    @EnvironmentObject private var store: TodoStore
    @State private var showAddAlert = false
    @State private var newTitle = ""
    @State private var showCompleted = false
    @State private var showEditSheet = false
    @State private var editingItem: TodoItem?
    @State private var showImporter = false
    @State private var showExporter = false
    @State private var importMessage: String? = nil

    var body: some View {
        ZStack {
            TranslucentBackground()

            VStack(spacing: 0) {
                // 标题栏
                HStack {
                    Text("TodoList")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundColor(.primary)
                    Spacer()

                    // 导入按钮
                    Button(action: { showImporter = true }) {
                        Image(systemName: "square.and.arrow.down")
                            .font(.system(size: 14))
                            .foregroundColor(.secondary)
                    }
                    .frame(width: 32, height: 32)

                    // 导出按钮
                    Button(action: { showExporter = true }) {
                        Image(systemName: "square.and.arrow.up")
                            .font(.system(size: 14))
                            .foregroundColor(.secondary)
                    }
                    .frame(width: 32, height: 32)

                    Button(action: { showCompleted.toggle() }) {
                        Image(systemName: showCompleted ? "eye.slash" : "eye")
                            .font(.system(size: 15))
                            .foregroundColor(.secondary)
                    }
                    .frame(width: 32, height: 32)
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 10)
                .background(.ultraThinMaterial.opacity(0.5))

                // 列表
                List {
                    if store.activeItems.isEmpty {
                        VStack(spacing: 8) {
                            Image(systemName: "checkmark.circle")
                                .font(.system(size: 36))
                                .foregroundColor(.secondary.opacity(0.5))
                            Text("暂无代办")
                                .font(.system(size: 15))
                                .foregroundColor(.secondary.opacity(0.6))
                        }
                        .frame(maxWidth: .infinity)
                        .listRowBackground(Color.clear)
                        .listRowSeparator(.hidden)
                        .padding(.vertical, 40)
                    }

                    ForEach(store.activeItems) { item in
                        TodoRowView(item: item, onEdit: { editingItem = item; showEditSheet = true })
                            .listRowInsets(EdgeInsets())
                            .listRowSeparator(.hidden)
                            .listRowBackground(Color.clear)
                            .swipeActions(edge: .trailing) {
                                Button(role: .destructive) {
                                    withAnimation { store.delete(item) }
                                } label: {
                                    Label("删除", systemImage: "trash")
                                }
                            }
                            .contextMenu {
                                Button { editingItem = item; showEditSheet = true } label: {
                                    Label("编辑", systemImage: "pencil")
                                }
                                Button { store.moveUp(item) } label: {
                                    Label("上移", systemImage: "arrow.up")
                                }
                                Button { store.moveDown(item) } label: {
                                    Label("下移", systemImage: "arrow.down")
                                }
                                Divider()
                                Button(role: .destructive) { store.delete(item) } label: {
                                    Label("删除", systemImage: "trash")
                                }
                            }
                    }

                    // 已完成（折叠）
                    if showCompleted && !store.completedItems.isEmpty {
                        Section {
                            ForEach(store.completedItems) { item in
                                HStack(spacing: 10) {
                                    Image(systemName: "checkmark.circle.fill")
                                        .foregroundColor(.green)
                                        .font(.system(size: 18))
                                    Text(item.title)
                                        .strikethrough()
                                        .foregroundColor(.secondary)
                                        .font(.system(size: 14))
                                }
                                .padding(.vertical, 6)
                                .padding(.horizontal, 14)
                                .listRowInsets(EdgeInsets())
                                .listRowSeparator(.hidden)
                                .listRowBackground(Color.clear)
                            }
                        } header: {
                            Text("已完成 (\(store.completedItems.count))")
                                .font(.system(size: 12, weight: .medium))
                                .foregroundColor(.secondary)
                                .textCase(nil)
                        }
                    }
                }
                .listStyle(.plain)
                .scrollContentBackground(.hidden)
                .environment(\.defaultMinListRowHeight, 1)

                // 添加按钮
                Button(action: { showAddAlert = true }) {
                    Label("添加待办", systemImage: "plus.circle.fill")
                        .font(.system(size: 15, weight: .medium))
                        .foregroundColor(.accentColor)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                        .background(.ultraThinMaterial.opacity(0.6))
                }
            }
        }
        .alert("添加待办", isPresented: $showAddAlert) {
            TextField("输入内容", text: $newTitle)
            Button("取消", role: .cancel) { newTitle = "" }
            Button("添加") {
                let t = newTitle.trimmingCharacters(in: .whitespaces)
                if !t.isEmpty { store.add(title: t) }
                newTitle = ""
            }
        } message: {
            Text("请输入待办事项内容")
        }
        .sheet(isPresented: $showEditSheet) {
            if let item = editingItem {
                EditTodoView(item: item)
            }
        }
        .fileImporter(
            isPresented: $showImporter,
            allowedContentTypes: [.item],
            allowsMultipleSelection: false
        ) { result in
            switch result {
            case .success(let urls):
                guard let url = urls.first else { return }
                guard url.startAccessingSecurityScopedResource() else { return }
                defer { url.stopAccessingSecurityScopedResource() }
                if let data = try? Data(contentsOf: url),
                   let text = String(data: data, encoding: .utf8) {
                    let count = store.importFromMarkdown(text)
                    importMessage = "导入了 \(count) 条新待办"
                } else {
                    importMessage = "导入失败：文件读取错误"
                }
            case .failure:
                importMessage = "导入失败"
            }
        }
        .fileExporter(
            isPresented: $showExporter,
            document: TextFileDocument(text: store.exportToMarkdown()),
            contentType: .plainText,
            defaultFilename: "PinToDesk-导出"
        ) { result in
            switch result {
            case .success: importMessage = "导出成功"
            case .failure: importMessage = "导出失败"
            }
        }
        .alert("提示", isPresented: .init(
            get: { importMessage != nil },
            set: { if !$0 { importMessage = nil } }
        )) {
            Button("确定") { importMessage = nil }
        } message: {
            Text(importMessage ?? "")
        }
    }
}

struct TranslucentBackground: View {
    var body: some View {
        Color.clear
            .background(
                VisualEffectView(effect: UIBlurEffect(style: .systemUltraThinMaterial))
                    .opacity(0.92)
            )
            .ignoresSafeArea()
    }
}

struct VisualEffectView: UIViewRepresentable {
    let effect: UIVisualEffect
    func makeUIView(context: Context) -> UIVisualEffectView { UIVisualEffectView(effect: effect) }
    func updateUIView(_ uiView: UIVisualEffectView, context: Context) { }
}

struct TextFileDocument: FileDocument {
    static var readableContentTypes: [UTType] { [.plainText] }
    var text: String

    init(text: String) { self.text = text }

    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents,
              let string = String(data: data, encoding: .utf8)
        else { throw CocoaError(.fileReadCorruptFile) }
        text = string
    }

    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper {
        FileWrapper(regularFileWithContents: Data(text.utf8))
    }
}
