# Subscribing to events

Composing lists of composed elements or master-details type of outputs cannot be a one-step process. If it was, there is a high chance of turning it into a select N+1 over HTTP type of scenario.

> [!NOTE]
> For an introduction to composing lists and the related challenges, read the [Into the darkness of ViewModels Lists Composition](https://milestone.topics.it/2019/02/28/into-the-darkness-of-viewmodel-lists-composition.html) blog post.

ServiceComposer offer two APIs to subscribe to events.

## Generic event handlers

snippet: generic-event-handler

## Route based subscribers