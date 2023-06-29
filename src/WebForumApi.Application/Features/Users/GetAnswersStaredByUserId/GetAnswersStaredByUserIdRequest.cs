using MediatR;
using System;
using WebForumApi.Application.Common.Requests;
using WebForumApi.Application.Common.Responses;
using WebForumApi.Application.Features.Questions.Dto;

namespace WebForumApi.Application.Features.Users.GetAnswersStaredByUserId;

public record GetAnswersStaredByUserIdRequest(Guid Id) : PaginatedRequest, IRequest<PaginatedList<AnswerCardDto>>;