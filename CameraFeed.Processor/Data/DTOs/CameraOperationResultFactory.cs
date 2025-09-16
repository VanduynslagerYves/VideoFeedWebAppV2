using Microsoft.AspNetCore.Mvc;

namespace CameraFeed.Processor.Data.DTOs;

public static class ResponseMessages
{
    public const string CameraAlreadyRunning = "Camera {0} is already running.";
    public const string CameraStarted = "Camera {0} has been started.";
    public const string CameraStartFailed = "Camera {0} failed to start.";
    public const string GeneralError = "An error occurred while processing your request for camera {0}.";
    public const string CameraStopped = "Camera {0} has been stopped.";
    public const string CameraNotRunning = "Camera {0} is not running.";
}

/// <summary>
/// Provides factory methods for creating JSON results representing the outcome of camera operations.
/// </summary>
/// <remarks>This static class is used to generate <see cref="JsonResult"/> objects that encapsulate the result of
/// operations performed on cameras, such as starting a camera. The result includes information about the success of the
/// operation, the camera identifier, and a message describing the operation's outcome.</remarks>
public static class CameraOperationResultFactory
{
    public static JsonResult Create(int cameraId, string message)
    {
        CameraOperationResultDto result;

        switch (message)
        {
            case ResponseMessages.CameraAlreadyRunning:
                result = new CameraOperationResultDto
                {
                    Success = true,
                    CameraId = cameraId,
                    Message = GetFormattedMessage(ResponseMessages.CameraAlreadyRunning, cameraId)
                };
                break;
            case ResponseMessages.CameraStarted:
                result = new CameraOperationResultDto
                {
                    Success = true,
                    CameraId = cameraId,
                    Message = GetFormattedMessage(ResponseMessages.CameraStarted, cameraId)
                };
                break;
            case ResponseMessages.CameraStartFailed:
                result = new CameraOperationResultDto
                {
                    Success = false,
                    CameraId = cameraId,
                    Message = GetFormattedMessage(ResponseMessages.CameraStartFailed, cameraId)
                };
                break;
            case ResponseMessages.CameraStopped:
                result = new CameraOperationResultDto
                {
                    Success = true,
                    CameraId = cameraId,
                    Message = GetFormattedMessage(ResponseMessages.CameraStopped, cameraId)
                };
                break;
            case ResponseMessages.CameraNotRunning:
                result = new CameraOperationResultDto
                {
                    Success = false,
                    CameraId = cameraId,
                    Message = GetFormattedMessage(ResponseMessages.CameraNotRunning, cameraId)
                };
                break;
            default:
                result = new CameraOperationResultDto
                {
                    Success = false,
                    CameraId = cameraId,
                    Message = GetFormattedMessage(ResponseMessages.GeneralError, cameraId)
                };
                break;
        }

        return new JsonResult(result);
    }

    private static string GetFormattedMessage(string message, int cameraId)
    {
        return string.Format(message, cameraId);
    }
}
