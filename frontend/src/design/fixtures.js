// DEVELOPMENT-ONLY DESIGN FIXTURES. Imported only by the development design review.

export const sampleUser = {
  id: 'design-self',
  name: 'Minh Anh',
}
export const people = [
  {
    id: 'design-1',
    otherUserId: 'design-jamie',
    name: 'Gia Huy',
    username: 'jamiechen',
    subtitle: 'Một chút cảm hứng cho cuối tuần',
    time: '10:42',
    tone: 1,
    unread: 2,
    status: 'Active',
  },
  {
    id: 'design-2',
    otherUserId: 'design-sam',
    name: 'Tuấn Minh',
    username: 'samrivera',
    subtitle: 'Nghe hợp lý đấy!',
    time: '09:18',
    tone: 2,
    unread: 0,
    status: 'Active',
  },
  {
    id: 'design-3',
    otherUserId: 'design-noor',
    name: 'Ngọc Hà',
    username: 'noorw',
    subtitle: 'Tuần này cà phê nhé?',
    time: 'Hôm qua',
    tone: 3,
    unread: 0,
    status: 'Active',
  },
  {
    id: 'design-4',
    otherUserId: 'design-theo',
    name: 'Đức Phúc',
    username: 'theopark',
    subtitle: 'Cảm ơn bạn đã chia sẻ',
    time: 'Hôm qua',
    tone: 4,
    unread: 0,
    status: 'Active',
  },
]
export const sampleGroup = {
  id: 'design-group',
  name: 'Góc sáng tạo',
  subtitle: 'Ý tưởng nhỏ. Kết nối vui.',
  description: 'Nơi chia sẻ ý tưởng, đặt câu hỏi và tìm cảm hứng mỗi ngày.',
  status: 'Active',
  myRole: 'Member',
  tone: 3,
  time: '11:06',
  unread: 3,
}
export const groupMembers = [
  {
    userId: 'design-jamie',
    displayName: 'Gia Huy',
    role: 'Owner',
  },
  {
    userId: 'design-sam',
    displayName: 'Tuấn Minh',
    role: 'Admin',
  },
  {
    userId: sampleUser.id,
    displayName: sampleUser.name,
    role: 'Member',
  },
  {
    userId: 'design-noor',
    displayName: 'Ngọc Hà',
    role: 'Member',
  },
]
export function sampleMessage(id, senderUserId, content, sequence, minute) {
  return {
    id: `design-message-${id}`,
    senderUserId,
    content,
    sequence,
    createdAt: `2026-11-06T03:${minute}:00Z`,
    recalled: false,
    previewCanEdit: [6, 13].includes(id),
  }
}
export const privateMessages = [
  sampleMessage(
    1,
    'design-jamie',
    'Minh Anh ơi, cuối tuần này nhớ dành thời gian cho điều bạn thích nhé.',
    1,
    '30',
  ),
  sampleMessage(2, sampleUser.id, 'Đúng lúc mình cần nghe lời này. Tuần vừa rồi bận quá.', 2, '32'),
  sampleMessage(
    3,
    'design-jamie',
    'Mình cũng vậy. Chỉ muốn một buổi sáng thong thả, cà phê ngon và không phải vội.',
    3,
    '34',
  ),
  sampleMessage(4, sampleUser.id, 'Nghe đúng kiểu cuối tuần mình mong đợi.', 4, '35'),
  sampleMessage(
    5,
    'design-jamie',
    'Có tiệm sách nhỏ gần công viên mình muốn ghé lâu rồi. Bạn đi cùng không?',
    5,
    '40',
  ),
  sampleMessage(
    6,
    sampleUser.id,
    'Cà phê và sách thì mình đi ngay. Thứ Bảy lúc 10 giờ nhé?',
    6,
    '42',
  ),
]
export const groupMessages = [
  sampleMessage(
    11,
    'design-jamie',
    'Tuần này có điều nhỏ bé nào khiến mọi người thấy có cảm hứng không?',
    1,
    '50',
  ),
  sampleMessage(
    12,
    'design-sam',
    'Hôm qua mình đi đường khác về nhà và tìm thấy một khu vườn nhỏ. Đôi khi chỉ cần đổi đường một chút.',
    2,
    '52',
  ),
  sampleMessage(
    13,
    sampleUser.id,
    'Hay quá. Mình bắt đầu ghi lại những ý tưởng nhỏ vào sổ để không quên mất.',
    3,
    '55',
  ),
  sampleMessage(14, 'design-noor', 'Một cuốn sổ đầy ý tưởng. Nghe thật thú vị.', 4, '58'),
  sampleMessage(
    15,
    'design-jamie',
    'Vì thế mình rất thích góc nhỏ này. Mọi người cứ tiếp tục chia sẻ nhé.',
    5,
    '59',
  ),
]
