# Surgeon
surgery-step-amputation-success-surgeon-popup = You set {$target}'s {$part}
surgery-step-amputation-success-self-surgeon-popup = You set your {$part}
surgery-step-amputation-success-no-zone-popup = You set {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-amputation-success-target-popup = {$user} sets your {$part}

# Outsider
surgery-step-amputation-success-outsider-popup = {$user} sets {$target}'s {$part}
surgery-step-amputation-success-self-outsider-popup = {$user} sets {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-amputation-begin-no-zone-outsider-popup = {$user} sets {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
