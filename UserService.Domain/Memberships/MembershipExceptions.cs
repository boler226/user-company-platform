namespace UserService.Domain.Memberships;

public class ResourceNotFoundException(string message) : Exception(message);
public class BusinessConflictException(string message) : Exception(message);
public class InputValidationException(string message) : Exception(message);
