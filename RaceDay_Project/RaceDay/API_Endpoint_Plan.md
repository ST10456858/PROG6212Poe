# RaceDay – API Endpoint Plan

This plan covers Authentication, User Profile, Events, Categories, Event Enrolments,
and Results, per the Part 1 requirements. It will be matched closely by the
implementation in Part 2.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None (public) | `{ fullName, email, password, role }` | 201 Created – user summary + JWT token<br>400 Bad Request – validation failed<br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and issues a JWT access token. | None (public) | `{ email, password }` | 200 OK – JWT token + user summary<br>401 Unauthorized – invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK – profile object<br>401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any (logged in) | `{ fullName, contactNumber, dateOfBirth, emergencyContact, organisationName }` (fields relevant to role) | 200 OK – updated profile<br>400 Bad Request<br>401 Unauthorized |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events so Participants can browse them. Supports optional filtering (e.g. by date or location). | None (public) | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns a single event's full details, including its categories. | None (public) | None | 200 OK – event object<br>404 Not Found |
| POST | /api/events | Creates a new event. | Organiser | `{ eventName, description, eventDate, startTime, location, routeMapUrl }` | 201 Created – event object<br>400 Bad Request<br>401 Unauthorized |
| PUT | /api/events/{id} | Updates an event the Organiser owns. | Organiser (owner) | `{ eventName, description, eventDate, startTime, location, routeMapUrl }` | 200 OK – updated event<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/events/{id} | Deletes an event the Organiser owns. | Organiser (owner) | None | 204 No Content<br>403 Forbidden<br>404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories for a given event (e.g. 10km, 21km). | None (public) | None | 200 OK – array of categories<br>404 Not Found |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. | Organiser (owner) | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 201 Created – category object<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| PUT | /api/categories/{id} | Updates a category. | Organiser (owner) | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 200 OK – updated category<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category. | Organiser (owner) | None | 204 No Content<br>403 Forbidden<br>404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enters the logged-in Participant into a category of an event. | Participant | None (participant identity taken from JWT) | 201 Created – enrolment object<br>400 Bad Request – category full<br>401 Unauthorized<br>409 Conflict – already enrolled |
| GET | /api/users/me/enrolments | Returns the logged-in Participant's own enrolment history. | Participant | None | 200 OK – array of enrolments |
| GET | /api/events/{eventId}/enrolments | Returns all enrolments for an event, for the Organiser who owns it. | Organiser (owner) | None | 200 OK – array of enrolments<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/enrolments/{id} | Cancels a Participant's own enrolment. | Participant (owner) | None | 204 No Content<br>403 Forbidden<br>404 Not Found |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/results | Captures a result for a participant's enrolment. | Organiser (owner) | `{ finishTime, positionOverall, positionCategory, status }` | 201 Created – result object<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| PUT | /api/results/{id} | Updates a previously captured result. | Organiser (owner) | `{ finishTime, positionOverall, positionCategory, status }` | 200 OK – updated result<br>403 Forbidden<br>404 Not Found |
| GET | /api/users/me/results | Returns the logged-in Participant's personal performance history across events. | Participant | None | 200 OK – array of results |
| GET | /api/events/{eventId}/results | Returns the full results/leaderboard for an event. | None (public) | None | 200 OK – array of results<br>404 Not Found |
