using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class CourseTypeAppService : ICourseTypeAppService
{
    private readonly ICourseTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CourseTypeAppService(
        ICourseTypeRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CourseTypeDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var types = await _repository.GetAllAsync(cancellationToken);

        return types
            .Select(ToDto)
            .ToList();
    }

    public async Task<(bool Success, CourseTypeDto? Data, string Message)> CreateAsync(
    CourseTypeRequestDto request,
    CancellationToken cancellationToken = default)
{
    if (request.TypeId <= 0)
    {
        return (false, null, "课程类型ID必须大于0");
    }

    var typeName = request.TypeName?.Trim();

    if (string.IsNullOrWhiteSpace(typeName))
    {
        return (false, null, "课程类型名称不能为空");
    }

    var existing = await _repository.GetByIdAsync(
        request.TypeId,
        cancellationToken);

    if (existing is not null)
    {
        return (false, null, "课程类型ID已存在");
    }

    var entity = new CourseType
    {
        TypeId = request.TypeId,
        TypeName = typeName
    };

    await _repository.AddAsync(entity, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return (true, ToDto(entity), "创建成功");
}

    public async Task<(bool Success, CourseTypeDto? Data, string Message)> UpdateAsync(
        int typeId,
        CourseTypeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var typeName = request.TypeName?.Trim();

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return (false, null, "课程类型名称不能为空");
        }

        var entity = await _repository.GetByIdAsync(typeId, cancellationToken);

        if (entity is null)
        {
            return (false, null, "课程类型不存在");
        }

        entity.TypeName = typeName;

        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, ToDto(entity), "修改成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(
        int typeId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(typeId, cancellationToken);

        if (entity is null)
        {
            return (false, "课程类型不存在");
        }

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, "删除成功");
    }

    private static CourseTypeDto ToDto(CourseType entity)
    {
        return new CourseTypeDto
        {
            TypeId = entity.TypeId,
            TypeName = entity.TypeName
        };
    }
}
