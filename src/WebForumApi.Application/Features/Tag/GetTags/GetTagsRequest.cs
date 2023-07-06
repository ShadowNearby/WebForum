﻿using Ardalis.Result;
using MediatR;
using WebForumApi.Application.Common.Requests;
using WebForumApi.Application.Common.Responses;
using WebForumApi.Application.Features.Tag.Dto;

namespace WebForumApi.Application.Features.Tag.GetTags;

public record GetTagsRequest : PaginatedRequest, IRequest<Result<PaginatedList<TagDto>>>
{
    public string? Keyword { get; init; }
}