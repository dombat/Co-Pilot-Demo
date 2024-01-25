# GitHub Co-Pilot Demo

## File: BadCode
Has various vulnerabvilities, such as hard coded passowrds and SQL injection.
Use "ALT /" to get suggestions from GitHub Co-Pilot on either a method or the whole class.

## File: ExistingCode
Contains ASN decoder. I'm not entirely sure what ASN is, so ask co-pilot about ASN and what the code does.

## File: Pipeline
This has a comment at the to to give chat some context. For the demo, I only know "Classic Pipelines" in Azure DevOps, so want to use chat to recraete some steps in YAML.

## File: Poor Code
This code has a timer in it which isn't disposed. there are various other things such as bad names etc.
Ask co-pilot if the timer is exposed, or if there are problems with the code in general.

## File: Requires Tests
This file contains two simple methods. Just ask the chat to create tests.
I have already included MSTEst package so it will knwo from the context, but it can also crate tests in NUnit and others.