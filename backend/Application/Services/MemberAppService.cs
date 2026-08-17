using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MemberAppService : IMemberAppService
{
    private readonly IMemberRepository _memberRepository;

    public MemberAppService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(id, cancellationToken);
        return member is null ? null : MapToDto(member);
    }

    public async Task<IReadOnlyList<MemberDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // 统一在应用层兜底分页参数，避免控制器和仓储重复写同样的规则。
        pageNumber = pageNumber <= 0 ? PagingConstants.DefaultPageNumber : pageNumber;
        pageSize = pageSize <= 0 ? PagingConstants.DefaultPageSize : Math.Min(pageSize, PagingConstants.MaxPageSize);

        var members = await _memberRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        return members.Select(MapToDto).ToList();
    }

    private static MemberDto MapToDto(Member member)
    {
        // DTO 用来隔离实体本身，避免把导航属性和持久化细节直接暴露给接口层。
        return new MemberDto
        {
            MemberId = member.MemberId,
            Name = member.Name,
            PhoneNumber = member.PhoneNumber,
            IdCard = member.IdCard,
            MemberLevel = member.MemberLevel,
            Gender = member.Gender,
            Birthday = member.Birthday,
            RegisterDate = member.RegisterDate,
            Status = member.Status
        };
    }
}


