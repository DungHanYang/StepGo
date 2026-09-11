using System.Text.Json.Serialization;
using StepGo.Contracts.Common;
using StepGo.Contracts.Courses;
using StepGo.Contracts.FeeLedger;
using StepGo.Contracts.Governance;
using StepGo.Contracts.Identity;
using StepGo.Contracts.Notifications;
using StepGo.Contracts.Orders;
using StepGo.Contracts.Payouts;
using StepGo.Contracts.RefundTickets;

namespace StepGo.Contracts.Json;

/// <summary>
/// System.Text.Json source-generated serialization context for every StepGo.Contracts DTO.
/// Native AOT Lambda handlers must serialize through this context instead of reflection-based
/// JsonSerializer.Serialize/Deserialize&lt;T&gt; overloads.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ErrorResponseDto))]
[JsonSerializable(typeof(RegisterUserRequestDto))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(BankAccountDto))]
[JsonSerializable(typeof(SubmitTeacherVerificationRequestDto))]
[JsonSerializable(typeof(TeacherVerificationDto))]
[JsonSerializable(typeof(ReviewTeacherVerificationRequestDto))]
[JsonSerializable(typeof(RefundTierDto))]
[JsonSerializable(typeof(CreateCourseRequestDto))]
[JsonSerializable(typeof(CourseDto))]
[JsonSerializable(typeof(CreateOrderRequestDto))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(PaymentGatewayNotificationDto))]
[JsonSerializable(typeof(FeeCalculationResultDto))]
[JsonSerializable(typeof(PayoutBatchOrderLineDto))]
[JsonSerializable(typeof(PayoutBatchDto))]
[JsonSerializable(typeof(MarkBankRejectedRequestDto))]
[JsonSerializable(typeof(MarkPayoutBatchPaidRequestDto))]
[JsonSerializable(typeof(SubmitRefundTicketRequestDto))]
[JsonSerializable(typeof(ThreadMessageDto))]
[JsonSerializable(typeof(InternalNoteDto))]
[JsonSerializable(typeof(RefundTicketDto))]
[JsonSerializable(typeof(TeacherRefundDecisionRequestDto))]
[JsonSerializable(typeof(AdminArbitrationRequestDto))]
[JsonSerializable(typeof(AddThreadMessageRequestDto))]
[JsonSerializable(typeof(AddInternalNoteRequestDto))]
[JsonSerializable(typeof(ProposeFeeSettingRequestDto))]
[JsonSerializable(typeof(PlatformFeeSettingDto))]
[JsonSerializable(typeof(ChangeLogEntryDto))]
[JsonSerializable(typeof(PublishTermsVersionRequestDto))]
[JsonSerializable(typeof(TermsVersionDto))]
[JsonSerializable(typeof(RecordTeacherConsentRequestDto))]
[JsonSerializable(typeof(NotificationTemplateDto))]
[JsonSerializable(typeof(UpdateNotificationTemplateRequestDto))]
[JsonSerializable(typeof(NotificationRecordDto))]
[JsonSerializable(typeof(UpdateAccountNotificationSettingsRequestDto))]
[JsonSerializable(typeof(PagedResultDto<CourseDto>))]
[JsonSerializable(typeof(PagedResultDto<OrderDto>))]
[JsonSerializable(typeof(PagedResultDto<PayoutBatchDto>))]
[JsonSerializable(typeof(PagedResultDto<RefundTicketDto>))]
[JsonSerializable(typeof(PagedResultDto<ChangeLogEntryDto>))]
[JsonSerializable(typeof(PagedResultDto<NotificationRecordDto>))]
public partial class StepGoJsonContext : JsonSerializerContext;
