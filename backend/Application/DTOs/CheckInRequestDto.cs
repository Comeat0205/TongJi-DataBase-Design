namespace Application.DTOs;

public sealed class CheckInRequestDto
{
    /// <summary>用户自行选择的会员卡编号。</summary>
    public int CardId { get; init; }

    public int VenueId { get; init; } = 1;

    /// <summary>
    /// 可选：会员自助签到时传入，用于校验所选卡属于该会员。
    /// </summary>
    public int? MemberId { get; init; }
}
