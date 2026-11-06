import { readFile, writeFile } from 'node:fs/promises'
import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
import generatorPackage from '@babel/generator'
const traverse = traversePackage.default
const generate = generatorPackage.default
const fixtureCopy = {
  'Alex Morgan': 'Minh Anh', 'Jamie Chen': 'Gia Huy', 'Sam Rivera': 'Tuấn Minh', 'Noor Williams': 'Ngọc Hà', 'Theo Park': 'Đức Phúc',
  'A little inspiration for the weekend': 'Một chút cảm hứng cho cuối tuần', 'That sounds like a plan!': 'Nghe hợp lý đấy!', 'Catch up over coffee this week?': 'Tuần này cà phê nhé?', 'Thanks for sharing that with me': 'Cảm ơn bạn đã chia sẻ', 'Yesterday': 'Hôm qua',
  'The Creative Corner': 'Góc sáng tạo', 'Small ideas. Good company.': 'Ý tưởng nhỏ. Kết nối vui.', 'A place for small ideas, big questions, and a little everyday inspiration.': 'Nơi chia sẻ ý tưởng, đặt câu hỏi và tìm cảm hứng mỗi ngày.', 'Weekend Wanderers': 'Hẹn cuối tuần', 'The next adventure starts here.': 'Chuyến đi tiếp theo bắt đầu từ đây.',
  'Hey Alex! A small reminder to make room for something you enjoy this weekend.': 'Minh Anh ơi, cuối tuần này nhớ dành thời gian cho điều bạn thích nhé.',
  'I needed this. It’s been one of those weeks.': 'Đúng lúc mình cần nghe lời này. Tuần vừa rồi bận quá.',
  'Same here. I’m thinking a slow morning, good coffee, and absolutely no plans.': 'Mình cũng vậy. Chỉ muốn một buổi sáng thong thả, cà phê ngon và không phải vội.',
  'That sounds like the perfect kind of plan.': 'Nghe đúng kiểu cuối tuần mình mong đợi.',
  'There’s a little bookshop near the park I’ve been meaning to visit. Want to come along?': 'Có tiệm sách nhỏ gần công viên mình muốn ghé lâu rồi. Bạn đi cùng không?',
  'Coffee and books? Count me in. Saturday at 10?': 'Cà phê và sách thì mình đi ngay. Thứ Bảy lúc 10 giờ nhé?',
  'A question for the corner: what’s one small thing that inspired you this week?': 'Tuần này có điều nhỏ bé nào khiến mọi người thấy có cảm hứng không?',
  'I took the long way home yesterday and found a tiny community garden. Sometimes a different route is all you need.': 'Hôm qua mình đi đường khác về nhà và tìm thấy một khu vườn nhỏ. Đôi khi chỉ cần đổi đường một chút.',
  'Love that. I started keeping a notebook of little ideas instead of letting them disappear.': 'Hay quá. Mình bắt đầu ghi lại những ý tưởng nhỏ vào sổ để không quên mất.',
  'A notebook full of possibilities. That’s a good way to put it.': 'Một cuốn sổ đầy ý tưởng. Nghe thật thú vị.',
  'This is why I love this little space. Keep the ideas coming.': 'Vì thế mình rất thích góc nhỏ này. Mọi người cứ tiếp tục chia sẻ nhé.',
  'A new sample message arrived. Your reading position should stay where you left it.': 'Có tin nhắn minh họa mới. Vị trí bạn đang đọc vẫn được giữ nguyên.',
}
const path = 'src/design/DesignReview.jsx'
let source = await readFile(path, 'utf8')
const ast = parse(source, { sourceType: 'module', plugins: ['jsx'] })
traverse(ast, { StringLiteral(node) { if (fixtureCopy[node.node.value]) node.node.value = fixtureCopy[node.node.value] } })
source = generate(ast, { jsescOption: { minimal: true } }).code.replace('Sample history ${index + 1}. A little note from an earlier part of the conversation.', 'Tin nhắn minh họa số ${index + 1}. Câu chuyện từ phần lịch sử trước.')
await writeFile(path, source + '\n')
const additions = {
  common: { darkMode: 'Chuyển sang giao diện tối', lightMode: 'Chuyển sang giao diện sáng', you: 'bạn', account: 'Tài khoản', friends: 'Bạn bè', notifications: 'Thông báo', reports: 'Báo cáo', messages: 'Tin nhắn', groups: 'Nhóm', admin: 'Quản trị', unknownDevice: 'Thiết bị chưa xác định' },
  auth: { requiredIdentifier: 'Vui lòng nhập email hoặc tên đăng nhập.', requiredPassword: 'Vui lòng nhập mật khẩu.' },
  chat: { memberSummary: '{{count}} thành viên · {{role}}', membersLoading: 'Đang tải thông tin thành viên' },
}
for (const [ns, values] of Object.entries(additions)) {
  const file = `src/i18n/locales/vi/${ns}.json`
  await writeFile(file, JSON.stringify({ ...JSON.parse(await readFile(file, 'utf8')), ...values }, null, 2) + '\n')
}
// Dynamic pronoun is UI copy; actual authors and message content are never translated.
const chatFile = 'src/components/chat/ChatWorkspace.jsx'
let chat = await readFile(chatFile, 'utf8')
chat = chat.replace("own ? 'you' : author", "own ? t('common:you') : author")
await writeFile(chatFile, chat)
