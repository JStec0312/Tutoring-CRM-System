# Tutoring Management System — Domain Glossary

## Document status

The glossary describes the final domain model for the current project scope.

The domain model intentionally omits database identifiers, foreign keys, table names, API contracts, persistence details and infrastructure-specific classes.

---

# 1. Identity and Access

## UserAccount

**Type:** Aggregate Root

**Domain meaning:**  
Represents an account used to authenticate a person and authorize access to the system. A single account may have more than one role.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `email` | `EmailAddress` | Email used to identify and contact the account owner |
| `roles` | `Set<UserRole>` | Roles assigned to the account |
| `status` | `AccountStatus` | Current account state |
| `profile` | `PersonalProfile` | Personal information of the account owner |

**Connections:**

- Owns one `EmailAddress`.
- Owns one `PersonalProfile`.
- May be connected with one `Tutor`.
- May be connected with one `Student`.
- Every `Tutor` and `Student` must have exactly one `UserAccount`.

---

## PersonalProfile

**Type:** Value Object

**Domain meaning:**  
Contains personal information shared by the roles associated with a user account.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `firstName` | `String` | First name of the account owner |
| `lastName` | `String` | Last name of the account owner |
| `phoneNumber` | `PhoneNumber` | Optional contact phone number |

**Connections:**

- Belongs to one `UserAccount`.
- May own one `PhoneNumber`.

---

## EmailAddress

**Type:** Value Object

**Domain meaning:**  
Represents a validated and normalized email address.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `value` | `String` | Email address value |

**Connections:**

- Belongs to `UserAccount`.
- Is used as the recipient of `StudentInvitation`.

---

## PhoneNumber

**Type:** Value Object

**Domain meaning:**  
Represents a validated and normalized telephone number.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `value` | `String` | Telephone number value |

**Connections:**

- May belong to `PersonalProfile`.

---

## UserRole

**Type:** Enumeration

**Domain meaning:**  
Defines a system role assigned to a user account.

**Expected values:**

- `Tutor`
- `Student`
- `Administrator`

An account may have multiple roles.

---

## AccountStatus

**Type:** Enumeration

**Domain meaning:**  
Defines whether a user account may currently access the system.

**Expected values:**

- `PendingActivation`
- `Active`
- `Suspended`
- `Deleted`

---

# 2. Tutoring Cooperation

## Tutor

**Type:** Aggregate Root

**Domain meaning:**  
Represents a user who provides tutoring services.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `status` | `TutorStatus` | Current business status of the tutor |

**Connections:**

- Must be connected with exactly one `UserAccount`.
- May participate in multiple `TutoringAgreement` aggregates.
- May send multiple `StudentInvitation` aggregates.
- May own multiple `LearningMaterial` aggregates.

---

## Student

**Type:** Aggregate Root

**Domain meaning:**  
Represents a user who receives tutoring services.

A student cannot exist without a user account.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `status` | `StudentStatus` | Current business status of the student |

**Connections:**

- Must be connected with exactly one `UserAccount`.
- May participate in multiple `TutoringAgreement` aggregates.

---

## TutoringAgreement

**Type:** Aggregate Root

**Domain meaning:**  
Represents cooperation between one tutor and one student for one subject.

The agreement stores conditions that belong to a specific cooperation rather than directly to the tutor or student.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `subject` | `Subject` | Subject taught under the agreement |
| `hourlyRate` | `HourlyRate` | Individual price per tutoring hour |
| `status` | `AgreementStatus` | Current state of the cooperation |
| `privateNotes` | `String` | Tutor's private notes about the cooperation |

**Connections:**

- Refers to exactly one `Tutor`.
- Refers to exactly one `Student`.
- Owns one `Subject`.
- Owns one `HourlyRate`.
- May contain multiple `Lesson` aggregates.
- Is settled by one `BillingAccount`.
- May receive multiple `MaterialAssignment` aggregates.

---

## StudentInvitation

**Type:** Aggregate Root

**Domain meaning:**  
Represents an invitation sent by a tutor to a future student.

Because every student must have an account, accepting an invitation creates or activates the required account and student profile before starting cooperation.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `recipient` | `EmailAddress` | Email address of the invited person |
| `status` | `InvitationStatus` | Current invitation state |
| `validUntil` | `DateTime` | Date and time when the invitation expires |

**Connections:**

- Is sent by exactly one `Tutor`.
- Acceptance creates or activates one `UserAccount`.
- Acceptance creates one `Student`.
- Acceptance may start one `TutoringAgreement`.

---

## Subject

**Type:** Value Object

**Domain meaning:**  
Represents the subject taught under a tutoring agreement.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `name` | `String` | Subject name |

**Connections:**

- Belongs to one `TutoringAgreement`.

---

## HourlyRate

**Type:** Value Object

**Domain meaning:**  
Represents the price charged for one hour of tutoring under a specific agreement.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `pricePerHour` | `Money` | Monetary value charged per hour |

**Connections:**

- Belongs to one `TutoringAgreement`.
- Owns one `Money`.

---

# 3. Lesson Planning

## Lesson

**Type:** Aggregate Root

**Domain meaning:**  
Represents a tutoring session scheduled within one tutoring agreement.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `timeSlot` | `TimeSlot` | Scheduled start and end of the lesson |
| `status` | `LessonStatus` | Current lesson state |
| `cancellationReason` | `CancellationReason` | Optional reason for cancellation |

**Connections:**

- Refers to exactly one `TutoringAgreement`.
- Owns one `TimeSlot`.
- May own one `CancellationReason`.
- May own one `LessonNote`.
- May be used by `BillingAccount` when a charge is created.
- May be referenced by `MaterialAssignment`.

---

## LessonNote

**Type:** Entity

**Domain meaning:**  
Represents information recorded about a specific lesson.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `topic` | `String` | Main lesson topic |
| `coveredTopics` | `String` | Issues covered during the lesson |
| `studentProgress` | `String` | Tutor's assessment of student progress |

**Connections:**

- Belongs to one `Lesson`.
- Does not exist independently outside the lesson aggregate.

---

## TimeSlot

**Type:** Value Object

**Domain meaning:**  
Represents the time interval during which a lesson takes place.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `startsAt` | `DateTime` | Lesson start |
| `endsAt` | `DateTime` | Lesson end |

**Connections:**

- Belongs to one `Lesson`.

---

## CancellationReason

**Type:** Value Object

**Domain meaning:**  
Explains why a lesson was cancelled.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `text` | `String` | Cancellation explanation |

**Connections:**

- May belong to one `Lesson`.

---

# 4. Billing

## BillingAccount

**Type:** Aggregate Root

**Domain meaning:**  
Represents the financial settlement of one tutoring agreement.

It coordinates lesson charges and payments.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `status` | `BillingAccountStatus` | Current settlement account state |

**Connections:**

- Refers to exactly one `TutoringAgreement`.
- Uses completed `Lesson` aggregates to create charges.
- Owns zero or more `LessonCharge` entities.
- Owns zero or more `Payment` entities.

---

## LessonCharge

**Type:** Entity

**Domain meaning:**  
Represents an amount charged for a completed or otherwise billable lesson.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `amount` | `Money` | Charged amount |
| `chargedAt` | `DateTime` | Charge creation date and time |
| `status` | `ChargeStatus` | Current charge state |
| `description` | `String` | Business description of the charge |

**Connections:**

- Belongs to one `BillingAccount`.
- Owns one `Money`.

---

## Payment

**Type:** Entity

**Domain meaning:**  
Represents an amount paid toward the settlement of a tutoring agreement.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `amount` | `Money` | Paid amount |
| `paidAt` | `DateTime` | Payment date and time |
| `reference` | `PaymentReference` | Optional payment identifier or description |

**Connections:**

- Belongs to one `BillingAccount`.
- Owns one `Money`.
- May own one `PaymentReference`.

---

## Money

**Type:** Value Object

**Domain meaning:**  
Represents an amount in a specific currency.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `amount` | `Decimal` | Numeric monetary value |
| `currency` | `Currency` | Currency of the amount |

**Connections:**

- Belongs to `HourlyRate`.
- Belongs to `LessonCharge`.
- Belongs to `Payment`.
- Owns one `Currency`.

---

## Currency

**Type:** Value Object

**Domain meaning:**  
Represents a currency used by a monetary value.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `code` | `String` | Currency code, for example `PLN`, `EUR` or `USD` |

**Connections:**

- Belongs to one `Money`.

---

## PaymentReference

**Type:** Value Object

**Domain meaning:**  
Represents an optional identifier or description assigned to a payment.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `value` | `String` | Payment reference value |

**Connections:**

- May belong to one `Payment`.

---

# 5. Learning Materials

## LearningMaterial

**Type:** Aggregate Root

**Domain meaning:**  
Represents a learning file uploaded and managed by a tutor.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `title` | `String` | Material title displayed to users |
| `file` | `FileDescriptor` | Description of the stored file |
| `status` | `MaterialStatus` | Current material state |

**Connections:**

- Is owned by exactly one `Tutor`.
- Owns one `FileDescriptor`.
- May be referenced by multiple `MaterialAssignment` aggregates.

---

## MaterialAssignment

**Type:** Aggregate Root

**Domain meaning:**  
Represents access granted to a learning material within a tutoring agreement.

The material may optionally be attached to a specific lesson.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `status` | `AssignmentStatus` | Current access state |
| `assignedAt` | `DateTime` | Date and time when access was granted |

**Connections:**

- Refers to exactly one `LearningMaterial`.
- Refers to exactly one `TutoringAgreement`.
- May refer to one `Lesson`.

---

## FileDescriptor

**Type:** Value Object

**Domain meaning:**  
Describes a stored learning material file without containing the file content itself.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `fileName` | `String` | File name |
| `contentType` | `String` | MIME content type |
| `size` | `FileSize` | File size |
| `storageLocation` | `StorageLocation` | Logical storage location |

**Connections:**

- Belongs to one `LearningMaterial`.
- Owns one `FileSize`.
- Owns one `StorageLocation`.

---

## FileSize

**Type:** Value Object

**Domain meaning:**  
Represents the size of a stored file.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `bytes` | `Long` | File size expressed in bytes |

**Connections:**

- Belongs to one `FileDescriptor`.

---

## StorageLocation

**Type:** Value Object

**Domain meaning:**  
Represents the logical location used to retrieve a stored file.

**Fields:**

| Field | Type | Meaning |
|---|---|---|
| `value` | `String` | Storage key or internal location |

**Connections:**

- Belongs to one `FileDescriptor`.

---

# 6. Status enumerations

## TutorStatus

Expected values:

- `Active`
- `Suspended`
- `Inactive`

## StudentStatus

Expected values:

- `Active`
- `Inactive`
- `Archived`

## AgreementStatus

Expected values:

- `Draft`
- `Active`
- `Suspended`
- `Ended`

## InvitationStatus

Expected values:

- `Created`
- `Sent`
- `Accepted`
- `Rejected`
- `Expired`

## LessonStatus

Expected values:

- `Scheduled`
- `Completed`
- `Cancelled`
- `Missed`

## BillingAccountStatus

Expected values:

- `Active`
- `Suspended`
- `Closed`

## ChargeStatus

Expected values:

- `Active`
- `Cancelled`
- `Corrected`

## MaterialStatus

Expected values:

- `Draft`
- `Published`
- `Archived`
- `Deleted`

## AssignmentStatus

Expected values:

- `Active`
- `Revoked`
