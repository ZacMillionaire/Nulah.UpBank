# Shared API classes
Models within this directory are shared with Api calls, and are exposed as public if they're able to be considered as
independent from API contexts.

Generally this means any enum types used for various type flags as these require no reduction from a verbose API object response.

Some other classes that describe a concept in a similar enough way that makes sense to leave as is and gain no benefit from
being changed eg, `MoneyObject` as there's no further reduction from what the API gives us that enhances clarity will also
be included here.