namespace CompanyService.Domain.Exceptions;

public class DomainException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
