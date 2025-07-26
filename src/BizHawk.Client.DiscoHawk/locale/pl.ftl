## Closing tabs

tabs-close-button = Zamknij
tabs-close-tooltip = {$tabCount ->
    [one] Zamknij kartę
    [few] Zamknij {$tabCount} karty
   *[many] Zamknij { $tabCount } kart
}
tabs-close-warning = {$tabCount ->
    [few] Zostaną zamknięte {$tabCount} karty.
          Czy chcesz kontynuować?
   *[many] Zostanie zamkniętych {$tabCount} kart.
           Czy chcesz kontynuować?
}

## Syncing

-sync-brand-name = {$case ->
   *[nominative] Konto Firefox
    [genitive] Konta Firefox
    [accusative] Kontem Firefox
}

sync-dialog-title = {-sync-brand-name}
sync-headline-title =
    {-sync-brand-name}: Najlepszy sposób na to,
    aby mieć swoje dane zawsze przy sobie
sync-signedout-title =
    Zaloguj do {-sync-brand-name[genitive]}
