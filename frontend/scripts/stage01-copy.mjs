export default {
  common: { skipContent: 'Chuyển đến nội dung chính', mainNavigation: 'Điều hướng chính', mobileNavigation: 'Điều hướng trên điện thoại' },
  chat: { chooseGroup: 'Chọn một nhóm để bắt đầu chia sẻ và kết nối.', choosePrivate: 'Chọn một cuộc trò chuyện để gửi lời chào.' },
  auth: {
    signOut: 'Đăng xuất', backToLogin: 'Quay lại đăng nhập', accountNavigation: 'Các thao tác tài khoản', securityNote: 'MeChat bảo vệ phiên đăng nhập của bạn. Không chia sẻ mật khẩu hoặc mã xác nhận.', tokenHelp: 'Nhập mã được gửi trong email. Không chia sẻ mã này với người khác.',
    fields: { email: 'Email', username: 'Tên đăng nhập', identifier: 'Email hoặc tên đăng nhập', password: 'Mật khẩu', confirmPassword: 'Xác nhận mật khẩu', newPassword: 'Mật khẩu mới', currentPassword: 'Mật khẩu hiện tại', token: 'Mã xác nhận' },
    validation: { email: 'Địa chỉ email không hợp lệ.', passwordLength: 'Mật khẩu cần ít nhất 12 ký tự.', passwordMismatch: 'Mật khẩu xác nhận chưa trùng khớp.', usernameMin: 'Tên đăng nhập cần ít nhất 3 ký tự.', usernameMax: 'Tên đăng nhập không quá 50 ký tự.' },
    pages: {
      register: { title: 'Tạo tài khoản MeChat', description: 'Bắt đầu những cuộc trò chuyện của bạn. Mật khẩu cần ít nhất 12 ký tự.', submit: 'Tạo tài khoản', success: 'Đã tiếp nhận đăng ký. Vui lòng kiểm tra hộp thư và xác minh email trước khi đăng nhập.', link: 'Tạo tài khoản' },
      verifyEmail: { title: 'Xác minh email', description: 'Nhập mã xác nhận trong email để kích hoạt quyền đăng nhập.', submit: 'Xác minh email', success: 'Email đã được xác minh. Bạn có thể đăng nhập.', link: 'Xác minh email' },
      resendVerification: { title: 'Gửi lại mã xác minh', description: 'Nhập email bạn đã dùng để đăng ký.', submit: 'Gửi lại mã xác minh', success: 'Nếu email cần xác minh, hướng dẫn đã được chuẩn bị. Vui lòng kiểm tra hộp thư.', link: 'Gửi lại mã xác minh' },
      forgotPassword: { title: 'Bạn quên mật khẩu?', description: 'Nhập email để nhận hướng dẫn đặt lại mật khẩu.', submit: 'Gửi hướng dẫn', success: 'Nếu tài khoản tồn tại, hướng dẫn đặt lại mật khẩu đã được chuẩn bị. Vui lòng kiểm tra hộp thư.', link: 'Quên mật khẩu' },
      resetPassword: { title: 'Đặt lại mật khẩu', description: 'Dùng mã trong email và tạo mật khẩu mới có ít nhất 12 ký tự.', submit: 'Đặt lại mật khẩu', success: 'Đã đặt lại mật khẩu. Vui lòng đăng nhập bằng mật khẩu mới.', link: 'Đặt lại mật khẩu' },
      reactivate: { title: 'Kích hoạt lại tài khoản', description: 'Nhập thông tin của tài khoản đã tự vô hiệu hóa để kích hoạt lại.', submit: 'Kích hoạt lại', success: 'Tài khoản đã được kích hoạt lại. Bạn có thể đăng nhập.', link: 'Kích hoạt lại tài khoản' },
    },
    changePasswordTitle: 'Đổi mật khẩu', changePasswordDescription: 'Các phiên trên thiết bị khác sẽ được đăng xuất sau khi đổi mật khẩu.', changePassword: 'Đổi mật khẩu', passwordChanged: 'Đã đổi mật khẩu và đăng xuất các phiên khác.',
    deactivateTitle: 'Vô hiệu hóa tài khoản', deactivateDescription: 'Tài khoản sẽ ngừng hoạt động và các phiên đăng nhập sẽ kết thúc. Bạn có thể kích hoạt lại bằng thông tin đăng nhập.', deactivate: 'Vô hiệu hóa tài khoản', deactivateConfirmation: 'Chỉ tiếp tục khi bạn muốn ngừng sử dụng tài khoản này. Nhập mật khẩu để xác nhận.',
    sessionsTitle: 'Phiên đăng nhập', sessionsDescription: 'Kiểm tra các thiết bị đang đăng nhập và thu hồi phiên không còn sử dụng.', logoutOthers: 'Đăng xuất thiết bị khác', logoutOthersConfirmation: 'Tất cả phiên khác sẽ kết thúc. Phiên hiện tại vẫn được giữ.', noSessions: 'Không có phiên đăng nhập', currentSession: 'Phiên hiện tại', sessionCreated: 'Đăng nhập lúc {{date}}', sessionActive: 'Hoạt động gần nhất: {{date}}', revokeSession: 'Thu hồi phiên', revokeConfirmation: 'Phiên này sẽ kết thúc và thiết bị cần đăng nhập lại.',
  },
  users: {
    accountTitle: 'Tài khoản của bạn', accountDescription: 'Quản lý hồ sơ, quyền riêng tư và bảo mật.', settingsNavigation: 'Cài đặt tài khoản', tabs: { profile: 'Hồ sơ', privacy: 'Quyền riêng tư', security: 'Bảo mật', sessions: 'Phiên đăng nhập' }, invalidSection: 'Hãy chọn một mục cài đặt tài khoản.',
    profileTitle: 'Thông tin cá nhân', profileDescription: 'Chọn cách bạn xuất hiện trong các cuộc trò chuyện.', displayName: 'Tên hiển thị', bio: 'Giới thiệu', displayNameLimit: 'Tên hiển thị không quá 100 ký tự.', bioLimit: 'Giới thiệu không quá 500 ký tự.', profileSaved: 'Đã lưu hồ sơ.',
    usernameTitle: 'Tên đăng nhập', usernameDescription: 'Tên đăng nhập là duy nhất. Sau khi đổi, bạn cần chờ 30 ngày để đổi tiếp.', newUsername: 'Tên đăng nhập mới', lastUsernameChange: 'Lần đổi gần nhất: {{date}}', usernameSaved: 'Đã đổi tên đăng nhập.',
    emailTitle: 'Đổi email', emailDescription: 'Bạn cần xác nhận email mới bằng mã được gửi trong hộp thư.', newEmail: 'Email mới', requestEmailChange: 'Gửi yêu cầu đổi email', emailRequested: 'Đã gửi yêu cầu. Vui lòng kiểm tra hộp thư của email mới.', emailChangeToken: 'Mã xác nhận đổi email', confirmEmail: 'Xác nhận email mới', emailConfirmed: 'Đã xác nhận email mới.',
    privacyTitle: 'Quyền riêng tư', privacyDescription: 'Chọn cách mọi người có thể tìm thấy và liên hệ với bạn.', privacySaved: 'Đã lưu quyền riêng tư.', privacy: {
      showOnline: { title: 'Hiển thị trạng thái trực tuyến', description: 'Cho người khác biết khi bạn đang trực tuyến.' },
      showLastSeen: { title: 'Hiển thị lần hoạt động gần nhất', description: 'Cho phép xem thời gian bạn hoạt động gần nhất.' },
      allowStrangerMessages: { title: 'Nhận tin nhắn từ người lạ', description: 'Cho phép người chưa kết bạn gửi lời nhắn theo quy định của MeChat.' },
      allowGroupInvites: { title: 'Nhận lời mời vào nhóm', description: 'Cho phép người phù hợp mời bạn tham gia nhóm.' },
    },
  },
  design: { 'private-chat': 'Trò chuyện riêng', 'group-chat': 'Trò chuyện nhóm' },
}
