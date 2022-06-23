﻿using FluentValidation;
using HSMServer.Constants;
using HSMServer.Core.Cache;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HSMServer.Model.Validators
{
    public class NewProductNameValidator : AbstractValidator<string>
    {
        private readonly ITreeValuesCache _treeValuesCache;
        public NewProductNameValidator(ITreeValuesCache treeValuesCache)
        {
            _treeValuesCache = treeValuesCache;

            RuleFor(x => x)
                .NotNull()
                .WithMessage(ErrorConstants.NameNotNull)
                .Must(IsUniqueName)
                .WithMessage(ErrorConstants.NameUnique)
                .Matches(@"^[0-9a-zA-Z .,_\-=#:;%&*()]*$", RegexOptions.IgnoreCase)
                .WithMessage(ErrorConstants.ProductNameSymbols);
        }

        // TODO: Remove IsUniqName validation after fixing saving products in db (ProductName to Id)
        private bool IsUniqueName(string name)
        {
            var products = _treeValuesCache.GetTree();

            return products?.FirstOrDefault(x =>
                x.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase)) == null;
        }
    }
}