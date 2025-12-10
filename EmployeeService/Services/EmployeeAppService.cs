using EmployeeService.Models;
using EmployeeService.Messaging;
using EmployeeService.Repositories;
using SharedModels.Events;

namespace EmployeeService.Services;

public class EmployeeAppService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public EmployeeAppService(IEmployeeRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<List<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var employees = await _repository.GetAllAsync(cancellationToken);
        return employees.Select(MapToResponse).ToList();
    }

    public async Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        return employee == null ? null : MapToResponse(employee);
    }

    public async Task<EmployeeResponse> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken)
    {
        if (await _repository.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var employee = new Employee
        {
            EmployeeId = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Role = request.Role
        };

        await _repository.AddAsync(employee, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishEmployeeCreatedAsync(new EmployeeCreatedEvent
        {
            EmployeeId = employee.EmployeeId,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role
        }, cancellationToken);

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            return null;
        }

        if (await _repository.EmailExistsAsync(request.Email, id, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        employee.Name = request.Name;
        employee.Email = request.Email;
        employee.Role = request.Role;

        await _repository.UpdateAsync(employee, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return MapToResponse(employee);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            return false;
        }

        await _repository.DeleteAsync(employee, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static EmployeeResponse MapToResponse(Employee employee) =>
        new()
        {
            EmployeeId = employee.EmployeeId,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role
        };
}

