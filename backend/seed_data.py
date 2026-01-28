"""
Database Seed Data for FreeStays
Contains default email templates and settings that can be restored after deployment
"""

# After-Sale Email Templates - All 11 Languages
AFTERSALE_EMAIL_TEMPLATES = {
    "en": {
        "no_payment": """Hi {guest_name},

We noticed that your recent order hasn't been completed yet, so we just wanted to check in with you 😊

Sometimes payments don't go through due to a small technical issue, and we'd hate for you to miss out on your Freestays benefits. 
Your selected offer is still available, and you can complete your order anytime using the {booking_id} in your 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Your Dashboard</a>.

If you have any questions or need help, just reply to this {support_email} — we're happy to assist.

Looking forward to welcoming you!

Warm regards,
Freestays Support Team
{support_email}
{website_url}""",

        "stop_payment": """Hello {guest_name},

We noticed that your payment attempt didn't go through, so we wanted to reach out and make sure everything is okay.
This can happen for many reasons (bank security checks, expired cards, or connection issues). 
If you'd still like to activate your Freestays access, you can simply try again using the same {booking_id}.

Of course, if you ran into any issues or have questions before continuing, just let us know — we're here to help.

Hope to hear from you soon!

Best wishes,
Freestays Support Team
{support_email}
{website_url}""",

        "not_interested": """Hi {guest_name},

We just wanted to follow up after your recent interest in Freestays.
If now isn't the right time, no worries at all — we completely understand. 
Travel plans change, and our offers will still be here when you're ready.

If there's anything holding you back or if you'd like more information before deciding, feel free to reply to  {support_email}. 

We'd be happy to help or simply leave things open for the future.

Wishing you all the best,
Freestays Support Team
{support_email}
{website_url}""",

        "new_offers": """Hello {guest_name},

We hope you've been doing well!
We wanted to reach out because we've added new hotel offers and destinations to Freestays, and we thought you might be interested. 
It's a great time to plan a getaway and enjoy free hotel stays with only meals to pay.
If you'd like to take another look or have questions about what's new, just reply to {support_email} — we'd love to help you find the perfect stay.

Hope to welcome you back soon!

Kind regards,
Freestays Support Team
{support_email}
{website_url}"""
    },
    
    "nl": {
        "no_payment": """Hoi {guest_name},

We merkten op dat je recente bestelling nog niet is afgerond, dus we wilden even bij je checken 😊

Soms gaan betalingen niet door vanwege een klein technisch probleem, en we zouden het jammer vinden als je je Freestays voordelen misloopt. 
Je geselecteerde aanbieding is nog steeds beschikbaar, en je kunt je bestelling op elk moment afronden met de {booking_id} in je 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Dashboard</a>.

Als je vragen hebt of hulp nodig hebt, antwoord dan gewoon op {support_email} — we helpen je graag.

We kijken ernaar uit je te verwelkomen!

Hartelijke groeten,
Freestays Support Team
{support_email}
{website_url}""",

        "stop_payment": """Hallo {guest_name},

We merkten op dat je betalingspoging niet is gelukt, dus we wilden even contact opnemen om te kijken of alles in orde is.
Dit kan om veel redenen gebeuren (beveiligingscontroles van de bank, verlopen kaarten of verbindingsproblemen). 
Als je je Freestays toegang nog steeds wilt activeren, kun je het gewoon opnieuw proberen met dezelfde {booking_id}.

Natuurlijk, als je problemen bent tegengekomen of vragen hebt voordat je verdergaat, laat het ons weten — we zijn er om te helpen.

Hopelijk tot snel!

Met vriendelijke groeten,
Freestays Support Team
{support_email}
{website_url}""",

        "not_interested": """Hoi {guest_name},

We wilden even opvolgen na je recente interesse in Freestays.
Als dit niet het juiste moment is, geen probleem — we begrijpen het volledig. 
Reisplannen veranderen, en onze aanbiedingen zijn er nog steeds wanneer je er klaar voor bent.

Als er iets is dat je tegenhoudt of als je meer informatie wilt voordat je beslist, aarzel dan niet om te antwoorden op {support_email}. 

We helpen je graag of laten de dingen gewoon open voor de toekomst.

Het allerbeste toegewenst,
Freestays Support Team
{support_email}
{website_url}""",

        "new_offers": """Hallo {guest_name},

We hopen dat het goed met je gaat!
We wilden contact opnemen omdat we nieuwe hotelaanbiedingen en bestemmingen aan Freestays hebben toegevoegd, en we dachten dat je misschien geïnteresseerd zou zijn. 
Het is een geweldige tijd om een uitje te plannen en te genieten van gratis hotelovernachtingen met alleen maaltijden om te betalen.
Als je nog eens wilt kijken of vragen hebt over wat er nieuw is, antwoord dan gewoon op {support_email} — we helpen je graag het perfecte verblijf te vinden.

Hopelijk tot snel!

Met vriendelijke groeten,
Freestays Support Team
{support_email}
{website_url}"""
    },
    
    "de": {
        "no_payment": """Hallo {guest_name},

Wir haben bemerkt, dass Ihre letzte Bestellung noch nicht abgeschlossen wurde, also wollten wir uns bei Ihnen melden 😊

Manchmal gehen Zahlungen aufgrund eines kleinen technischen Problems nicht durch, und wir möchten nicht, dass Sie Ihre Freestays-Vorteile verpassen. 
Ihr ausgewähltes Angebot ist noch verfügbar, und Sie können Ihre Bestellung jederzeit mit der {booking_id} in Ihrem 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Dashboard</a> abschließen.

Wenn Sie Fragen haben oder Hilfe benötigen, antworten Sie einfach an {support_email} — wir helfen Ihnen gerne.

Wir freuen uns darauf, Sie zu begrüßen!

Mit freundlichen Grüßen,
Freestays Support Team
{support_email}
{website_url}""",

        "stop_payment": """Hallo {guest_name},

Wir haben bemerkt, dass Ihr Zahlungsversuch nicht durchgegangen ist, also wollten wir uns melden und sicherstellen, dass alles in Ordnung ist.
Dies kann aus vielen Gründen passieren (Sicherheitsprüfungen der Bank, abgelaufene Karten oder Verbindungsprobleme). 
Wenn Sie Ihren Freestays-Zugang noch aktivieren möchten, können Sie es einfach erneut mit derselben {booking_id} versuchen.

Natürlich, wenn Sie auf Probleme gestoßen sind oder Fragen haben, bevor Sie fortfahren, lassen Sie es uns wissen — wir sind hier, um zu helfen.

Wir hoffen, bald von Ihnen zu hören!

Mit besten Grüßen,
Freestays Support Team
{support_email}
{website_url}""",

        "not_interested": """Hallo {guest_name},

Wir wollten uns nach Ihrem kürzlichen Interesse an Freestays melden.
Wenn jetzt nicht der richtige Zeitpunkt ist, kein Problem — wir verstehen das vollkommen. 
Reisepläne ändern sich, und unsere Angebote werden noch hier sein, wenn Sie bereit sind.

Wenn Sie etwas zurückhält oder wenn Sie mehr Informationen möchten, bevor Sie sich entscheiden, antworten Sie gerne an {support_email}. 

Wir helfen Ihnen gerne oder lassen die Dinge einfach für die Zukunft offen.

Alles Gute,
Freestays Support Team
{support_email}
{website_url}""",

        "new_offers": """Hallo {guest_name},

Wir hoffen, es geht Ihnen gut!
Wir wollten uns melden, weil wir neue Hotelangebote und Reiseziele zu Freestays hinzugefügt haben, und wir dachten, Sie könnten interessiert sein. 
Es ist eine großartige Zeit, einen Ausflug zu planen und kostenlose Hotelübernachtungen mit nur Mahlzeiten zu bezahlen zu genießen.
Wenn Sie noch einmal schauen möchten oder Fragen zu den Neuheiten haben, antworten Sie einfach an {support_email} — wir helfen Ihnen gerne, den perfekten Aufenthalt zu finden.

Wir hoffen, Sie bald wieder begrüßen zu dürfen!

Mit freundlichen Grüßen,
Freestays Support Team
{support_email}
{website_url}"""
    },
    
    "fr": {
        "no_payment": """Bonjour {guest_name},

Nous avons remarqué que votre commande récente n'a pas encore été finalisée, nous voulions donc prendre de vos nouvelles 😊

Parfois, les paiements échouent en raison d'un petit problème technique, et nous ne voudrions pas que vous manquiez vos avantages Freestays. 
Votre offre sélectionnée est toujours disponible, et vous pouvez finaliser votre commande à tout moment en utilisant le {booking_id} dans votre 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Tableau de bord</a>.

Si vous avez des questions ou besoin d'aide, répondez simplement à {support_email} — nous serons ravis de vous aider.

Au plaisir de vous accueillir !

Cordialement,
L'équipe Support Freestays
{support_email}
{website_url}""",

        "stop_payment": """Bonjour {guest_name},

Nous avons remarqué que votre tentative de paiement n'a pas abouti, nous voulions donc vous contacter pour nous assurer que tout va bien.
Cela peut arriver pour de nombreuses raisons (vérifications de sécurité bancaire, cartes expirées ou problèmes de connexion). 
Si vous souhaitez toujours activer votre accès Freestays, vous pouvez simplement réessayer avec le même {booking_id}.

Bien sûr, si vous avez rencontré des problèmes ou avez des questions avant de continuer, faites-le nous savoir — nous sommes là pour aider.

À bientôt !

Meilleures salutations,
L'équipe Support Freestays
{support_email}
{website_url}""",

        "not_interested": """Bonjour {guest_name},

Nous voulions faire un suivi après votre récent intérêt pour Freestays.
Si ce n'est pas le bon moment, pas de problème — nous comprenons parfaitement. 
Les plans de voyage changent, et nos offres seront toujours là quand vous serez prêt.

Si quelque chose vous retient ou si vous souhaitez plus d'informations avant de décider, n'hésitez pas à répondre à {support_email}. 

Nous serons heureux de vous aider ou simplement de laisser les choses ouvertes pour l'avenir.

Tous nos vœux,
L'équipe Support Freestays
{support_email}
{website_url}""",

        "new_offers": """Bonjour {guest_name},

Nous espérons que vous allez bien !
Nous voulions vous contacter car nous avons ajouté de nouvelles offres d'hôtels et destinations à Freestays, et nous avons pensé que cela pourrait vous intéresser. 
C'est le moment idéal pour planifier une escapade et profiter de séjours hôteliers gratuits avec seulement les repas à payer.
Si vous souhaitez jeter un autre coup d'œil ou avez des questions sur les nouveautés, répondez simplement à {support_email} — nous serons ravis de vous aider à trouver le séjour parfait.

Au plaisir de vous revoir bientôt !

Cordialement,
L'équipe Support Freestays
{support_email}
{website_url}"""
    },
    
    "es": {
        "no_payment": """Hola {guest_name},

Hemos notado que tu pedido reciente aún no se ha completado, así que queríamos contactarte 😊

A veces los pagos no se procesan debido a un pequeño problema técnico, y no queremos que te pierdas tus beneficios de Freestays. 
Tu oferta seleccionada sigue disponible, y puedes completar tu pedido en cualquier momento usando el {booking_id} en tu 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Panel de control</a>.

Si tienes alguna pregunta o necesitas ayuda, simplemente responde a {support_email} — estaremos encantados de ayudarte.

¡Esperamos darte la bienvenida pronto!

Saludos cordiales,
Equipo de Soporte Freestays
{support_email}
{website_url}""",

        "stop_payment": """Hola {guest_name},

Hemos notado que tu intento de pago no se procesó, así que queríamos contactarte para asegurarnos de que todo está bien.
Esto puede suceder por muchas razones (verificaciones de seguridad del banco, tarjetas caducadas o problemas de conexión). 
Si aún deseas activar tu acceso a Freestays, puedes simplemente intentarlo de nuevo con el mismo {booking_id}.

Por supuesto, si has tenido algún problema o tienes preguntas antes de continuar, háznoslo saber — estamos aquí para ayudar.

¡Esperamos saber de ti pronto!

Saludos,
Equipo de Soporte Freestays
{support_email}
{website_url}""",

        "not_interested": """Hola {guest_name},

Queríamos hacer un seguimiento después de tu reciente interés en Freestays.
Si ahora no es el momento adecuado, no hay problema — lo entendemos completamente. 
Los planes de viaje cambian, y nuestras ofertas seguirán aquí cuando estés listo.

Si hay algo que te detiene o si deseas más información antes de decidir, no dudes en responder a {support_email}. 

Estaremos encantados de ayudarte o simplemente dejar las cosas abiertas para el futuro.

Te deseamos lo mejor,
Equipo de Soporte Freestays
{support_email}
{website_url}""",

        "new_offers": """Hola {guest_name},

¡Esperamos que estés bien!
Queríamos contactarte porque hemos añadido nuevas ofertas de hoteles y destinos a Freestays, y pensamos que podrían interesarte. 
Es un gran momento para planificar una escapada y disfrutar de estancias de hotel gratuitas pagando solo las comidas.
Si quieres echar otro vistazo o tienes preguntas sobre las novedades, simplemente responde a {support_email} — nos encantaría ayudarte a encontrar la estancia perfecta.

¡Esperamos verte pronto!

Saludos cordiales,
Equipo de Soporte Freestays
{support_email}
{website_url}"""
    },
    
    "it": {
        "no_payment": """Ciao {guest_name},

Abbiamo notato che il tuo ordine recente non è stato ancora completato, quindi volevamo contattarti 😊

A volte i pagamenti non vanno a buon fine a causa di un piccolo problema tecnico, e non vorremmo che perdessi i tuoi vantaggi Freestays. 
La tua offerta selezionata è ancora disponibile, e puoi completare il tuo ordine in qualsiasi momento usando il {booking_id} nel tuo 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Dashboard</a>.

Se hai domande o hai bisogno di aiuto, rispondi semplicemente a {support_email} — saremo felici di assisterti.

Non vediamo l'ora di darti il benvenuto!

Cordiali saluti,
Team Supporto Freestays
{support_email}
{website_url}""",

        "stop_payment": """Ciao {guest_name},

Abbiamo notato che il tuo tentativo di pagamento non è andato a buon fine, quindi volevamo contattarti per assicurarci che tutto sia ok.
Questo può accadere per molti motivi (controlli di sicurezza bancari, carte scadute o problemi di connessione). 
Se desideri ancora attivare il tuo accesso Freestays, puoi semplicemente riprovare con lo stesso {booking_id}.

Naturalmente, se hai riscontrato problemi o hai domande prima di continuare, faccelo sapere — siamo qui per aiutarti.

Speriamo di sentirti presto!

I migliori saluti,
Team Supporto Freestays
{support_email}
{website_url}""",

        "not_interested": """Ciao {guest_name},

Volevamo fare un follow-up dopo il tuo recente interesse per Freestays.
Se ora non è il momento giusto, nessun problema — capiamo perfettamente. 
I piani di viaggio cambiano, e le nostre offerte saranno ancora qui quando sarai pronto.

Se c'è qualcosa che ti trattiene o se desideri maggiori informazioni prima di decidere, sentiti libero di rispondere a {support_email}. 

Saremo felici di aiutarti o semplicemente lasciare le cose aperte per il futuro.

Ti auguriamo il meglio,
Team Supporto Freestays
{support_email}
{website_url}""",

        "new_offers": """Ciao {guest_name},

Speriamo che tu stia bene!
Volevamo contattarti perché abbiamo aggiunto nuove offerte hotel e destinazioni a Freestays, e abbiamo pensato che potresti essere interessato. 
È un ottimo momento per pianificare una vacanza e goderti soggiorni in hotel gratuiti pagando solo i pasti.
Se vuoi dare un'altra occhiata o hai domande sulle novità, rispondi semplicemente a {support_email} — ci piacerebbe aiutarti a trovare il soggiorno perfetto.

Speriamo di rivederti presto!

Cordiali saluti,
Team Supporto Freestays
{support_email}
{website_url}"""
    },
    
    "pl": {
        "no_payment": """Cześć {guest_name},

Zauważyliśmy, że Twoje ostatnie zamówienie nie zostało jeszcze zrealizowane, więc chcieliśmy się z Tobą skontaktować 😊

Czasami płatności nie przechodzą z powodu małego problemu technicznego, i nie chcielibyśmy, żebyś stracił korzyści Freestays. 
Twoja wybrana oferta jest nadal dostępna, i możesz dokończyć zamówienie w dowolnym momencie używając {booking_id} w swoim 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Panelu</a>.

Jeśli masz pytania lub potrzebujesz pomocy, po prostu odpowiedz na {support_email} — chętnie pomożemy.

Nie możemy się doczekać, żeby Cię powitać!

Serdeczne pozdrowienia,
Zespół Wsparcia Freestays
{support_email}
{website_url}""",

        "stop_payment": """Cześć {guest_name},

Zauważyliśmy, że Twoja próba płatności nie powiodła się, więc chcieliśmy się skontaktować i upewnić, że wszystko jest w porządku.
Może się to zdarzyć z wielu powodów (kontrole bezpieczeństwa banku, wygasłe karty lub problemy z połączeniem). 
Jeśli nadal chcesz aktywować dostęp do Freestays, możesz po prostu spróbować ponownie używając tego samego {booking_id}.

Oczywiście, jeśli napotkałeś jakiekolwiek problemy lub masz pytania przed kontynuowaniem, daj nam znać — jesteśmy tu, aby pomóc.

Mamy nadzieję, że wkrótce się odezwiesz!

Z poważaniem,
Zespół Wsparcia Freestays
{support_email}
{website_url}""",

        "not_interested": """Cześć {guest_name},

Chcieliśmy się skontaktować po Twoim niedawnym zainteresowaniu Freestays.
Jeśli teraz nie jest odpowiedni moment, nie ma problemu — całkowicie rozumiemy. 
Plany podróży się zmieniają, a nasze oferty będą nadal dostępne, gdy będziesz gotowy.

Jeśli coś Cię powstrzymuje lub chciałbyś uzyskać więcej informacji przed podjęciem decyzji, śmiało odpowiedz na {support_email}. 

Chętnie pomożemy lub po prostu zostawimy sprawy otwarte na przyszłość.

Życzymy Ci wszystkiego najlepszego,
Zespół Wsparcia Freestays
{support_email}
{website_url}""",

        "new_offers": """Cześć {guest_name},

Mamy nadzieję, że wszystko u Ciebie dobrze!
Chcieliśmy się skontaktować, ponieważ dodaliśmy nowe oferty hoteli i destynacje do Freestays, i pomyśleliśmy, że może Cię to zainteresować. 
To świetny czas, żeby zaplanować wycieczkę i cieszyć się darmowymi pobytami w hotelach płacąc tylko za posiłki.
Jeśli chciałbyś jeszcze raz spojrzeć lub masz pytania o nowości, po prostu odpowiedz na {support_email} — chętnie pomożemy Ci znaleźć idealny pobyt.

Mamy nadzieję, że wkrótce Cię ponownie powitamy!

Pozdrawiamy serdecznie,
Zespół Wsparcia Freestays
{support_email}
{website_url}"""
    },
    
    "sv": {
        "no_payment": """Hej {guest_name},

Vi märkte att din senaste beställning inte har slutförts ännu, så vi ville höra av oss 😊

Ibland går betalningar inte igenom på grund av ett litet tekniskt problem, och vi vill inte att du ska missa dina Freestays-fördelar. 
Ditt valda erbjudande är fortfarande tillgängligt, och du kan slutföra din beställning när som helst med {booking_id} i din 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Kontrollpanel</a>.

Om du har några frågor eller behöver hjälp, svara bara på {support_email} — vi hjälper dig gärna.

Vi ser fram emot att välkomna dig!

Vänliga hälsningar,
Freestays Supportteam
{support_email}
{website_url}""",

        "stop_payment": """Hej {guest_name},

Vi märkte att ditt betalningsförsök inte gick igenom, så vi ville höra av oss och försäkra oss om att allt är okej.
Detta kan hända av många anledningar (banksäkerhetskontroller, utgångna kort eller anslutningsproblem). 
Om du fortfarande vill aktivera din Freestays-åtkomst kan du helt enkelt försöka igen med samma {booking_id}.

Självklart, om du har stött på några problem eller har frågor innan du fortsätter, låt oss veta — vi är här för att hjälpa.

Hoppas vi hörs snart!

Med vänliga hälsningar,
Freestays Supportteam
{support_email}
{website_url}""",

        "not_interested": """Hej {guest_name},

Vi ville följa upp efter ditt senaste intresse för Freestays.
Om det inte är rätt tidpunkt nu, inga problem — vi förstår helt. 
Reseplaner ändras, och våra erbjudanden kommer fortfarande att finnas här när du är redo.

Om något håller dig tillbaka eller om du vill ha mer information innan du bestämmer dig, svara gärna på {support_email}. 

Vi hjälper dig gärna eller lämnar helt enkelt saker öppna för framtiden.

Vi önskar dig allt gott,
Freestays Supportteam
{support_email}
{website_url}""",

        "new_offers": """Hej {guest_name},

Vi hoppas att du mår bra!
Vi ville höra av oss eftersom vi har lagt till nya hotellerbjudanden och destinationer till Freestays, och vi tänkte att du kanske är intresserad. 
Det är ett utmärkt tillfälle att planera en utflykt och njuta av gratis hotellvistelser med bara måltider att betala.
Om du vill ta en ny titt eller har frågor om nyheterna, svara bara på {support_email} — vi hjälper dig gärna att hitta den perfekta vistelsen.

Hoppas vi ses snart igen!

Vänliga hälsningar,
Freestays Supportteam
{support_email}
{website_url}"""
    },
    
    "da": {
        "no_payment": """Hej {guest_name},

Vi bemærkede, at din seneste ordre endnu ikke er gennemført, så vi ville lige tjekke ind hos dig 😊

Nogle gange går betalinger ikke igennem på grund af et lille teknisk problem, og vi vil nødig have, at du går glip af dine Freestays-fordele. 
Dit valgte tilbud er stadig tilgængeligt, og du kan gennemføre din ordre når som helst ved hjælp af {booking_id} i dit 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Kontrolpanel</a>.

Hvis du har spørgsmål eller brug for hjælp, så svar bare på {support_email} — vi hjælper gerne.

Vi glæder os til at byde dig velkommen!

Venlig hilsen,
Freestays Support Team
{support_email}
{website_url}""",

        "stop_payment": """Hej {guest_name},

Vi bemærkede, at dit betalingsforsøg ikke gik igennem, så vi ville kontakte dig for at sikre, at alt er i orden.
Dette kan ske af mange grunde (banksikkerhedstjek, udløbne kort eller forbindelsesproblemer). 
Hvis du stadig gerne vil aktivere din Freestays-adgang, kan du simpelthen prøve igen med det samme {booking_id}.

Selvfølgelig, hvis du er stødt på problemer eller har spørgsmål, før du fortsætter, så lad os vide — vi er her for at hjælpe.

Håber at høre fra dig snart!

Med venlig hilsen,
Freestays Support Team
{support_email}
{website_url}""",

        "not_interested": """Hej {guest_name},

Vi ville gerne følge op efter din seneste interesse for Freestays.
Hvis nu ikke er det rette tidspunkt, ingen bekymringer — vi forstår det fuldstændigt. 
Rejseplaner ændrer sig, og vores tilbud vil stadig være her, når du er klar.

Hvis noget holder dig tilbage, eller hvis du gerne vil have mere information, før du beslutter dig, er du velkommen til at svare på {support_email}. 

Vi hjælper dig gerne eller lader simpelthen tingene stå åbne for fremtiden.

Vi ønsker dig alt det bedste,
Freestays Support Team
{support_email}
{website_url}""",

        "new_offers": """Hej {guest_name},

Vi håber, du har det godt!
Vi ville gerne kontakte dig, fordi vi har tilføjet nye hoteltilbud og destinationer til Freestays, og vi tænkte, du måske kunne være interesseret. 
Det er et godt tidspunkt at planlægge en udflugt og nyde gratis hotelophold med kun måltider at betale.
Hvis du gerne vil kigge igen eller har spørgsmål om nyhederne, så svar bare på {support_email} — vi vil elske at hjælpe dig med at finde det perfekte ophold.

Håber at byde dig velkommen tilbage snart!

Venlig hilsen,
Freestays Support Team
{support_email}
{website_url}"""
    },
    
    "no": {
        "no_payment": """Hei {guest_name},

Vi la merke til at din siste bestilling ennå ikke er fullført, så vi ville bare sjekke inn med deg 😊

Noen ganger går ikke betalinger gjennom på grunn av et lite teknisk problem, og vi vil ikke at du skal gå glipp av Freestays-fordelene dine. 
Ditt valgte tilbud er fortsatt tilgjengelig, og du kan fullføre bestillingen din når som helst ved å bruke {booking_id} i ditt 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Kontrollpanel</a>.

Hvis du har spørsmål eller trenger hjelp, bare svar på {support_email} — vi hjelper deg gjerne.

Vi gleder oss til å ønske deg velkommen!

Vennlig hilsen,
Freestays Support Team
{support_email}
{website_url}""",

        "stop_payment": """Hei {guest_name},

Vi la merke til at betalingsforsøket ditt ikke gikk gjennom, så vi ville kontakte deg for å forsikre oss om at alt er i orden.
Dette kan skje av mange grunner (banksikkerhetssjekker, utløpte kort eller tilkoblingsproblemer). 
Hvis du fortsatt ønsker å aktivere Freestays-tilgangen din, kan du ganske enkelt prøve igjen med samme {booking_id}.

Selvfølgelig, hvis du har støtt på problemer eller har spørsmål før du fortsetter, gi oss beskjed — vi er her for å hjelpe.

Håper vi høres snart!

Med vennlig hilsen,
Freestays Support Team
{support_email}
{website_url}""",

        "not_interested": """Hei {guest_name},

Vi ville følge opp etter din nylige interesse for Freestays.
Hvis nå ikke er riktig tidspunkt, ingen bekymringer — vi forstår det fullstendig. 
Reiseplaner endrer seg, og tilbudene våre vil fortsatt være her når du er klar.

Hvis noe holder deg tilbake eller hvis du ønsker mer informasjon før du bestemmer deg, svar gjerne på {support_email}. 

Vi hjelper deg gjerne eller lar ganske enkelt ting være åpne for fremtiden.

Vi ønsker deg alt godt,
Freestays Support Team
{support_email}
{website_url}""",

        "new_offers": """Hei {guest_name},

Vi håper du har det bra!
Vi ville kontakte deg fordi vi har lagt til nye hotelltilbud og destinasjoner til Freestays, og vi tenkte du kanskje kunne være interessert. 
Det er en flott tid å planlegge en tur og nyte gratis hotellopphold med bare måltider å betale.
Hvis du vil ta en ny titt eller har spørsmål om nyhetene, bare svar på {support_email} — vi vil gjerne hjelpe deg med å finne det perfekte oppholdet.

Håper å ønske deg velkommen tilbake snart!

Med vennlig hilsen,
Freestays Support Team
{support_email}
{website_url}"""
    },
    
    "tr": {
        "no_payment": """Merhaba {guest_name},

Son siparişinizin henüz tamamlanmadığını fark ettik, bu yüzden sizinle iletişime geçmek istedik 😊

Bazen ödemeler küçük bir teknik sorun nedeniyle gerçekleşmeyebilir ve Freestays avantajlarınızı kaçırmanızı istemiyoruz. 
Seçtiğiniz teklif hala mevcut ve siparişinizi istediğiniz zaman {booking_id} kullanarak 👉 <a href="https://freestays.eu/dashboard" style="color:#0066cc;text-decoration:underline;">Kontrol Panelinizden</a> tamamlayabilirsiniz.

Herhangi bir sorunuz varsa veya yardıma ihtiyacınız varsa, {support_email} adresine yanıt verin — yardımcı olmaktan memnuniyet duyarız.

Sizi ağırlamayı dört gözle bekliyoruz!

Saygılarımızla,
Freestays Destek Ekibi
{support_email}
{website_url}""",

        "stop_payment": """Merhaba {guest_name},

Ödeme girişiminizin gerçekleşmediğini fark ettik, bu yüzden her şeyin yolunda olduğundan emin olmak için sizinle iletişime geçmek istedik.
Bu birçok nedenle olabilir (banka güvenlik kontrolleri, süresi dolmuş kartlar veya bağlantı sorunları). 
Freestays erişiminizi hala etkinleştirmek istiyorsanız, aynı {booking_id} ile tekrar deneyebilirsiniz.

Tabii ki, herhangi bir sorunla karşılaştıysanız veya devam etmeden önce sorularınız varsa, bize bildirin — yardım etmek için buradayız.

Yakında sizden haber almayı umuyoruz!

En iyi dileklerimizle,
Freestays Destek Ekibi
{support_email}
{website_url}""",

        "not_interested": """Merhaba {guest_name},

Freestays'e olan son ilginizden sonra takip etmek istedik.
Şu an doğru zaman değilse, sorun değil — tamamen anlıyoruz. 
Seyahat planları değişir ve tekliflerimiz hazır olduğunuzda hala burada olacak.

Sizi engelleyen bir şey varsa veya karar vermeden önce daha fazla bilgi istiyorsanız, {support_email} adresine yanıt vermekten çekinmeyin. 

Yardımcı olmaktan memnuniyet duyarız veya sadece gelecek için açık bırakırız.

Size en iyisini diliyoruz,
Freestays Destek Ekibi
{support_email}
{website_url}""",

        "new_offers": """Merhaba {guest_name},

Umarız iyisinizdir!
Freestays'e yeni otel teklifleri ve destinasyonlar eklediğimiz için sizinle iletişime geçmek istedik ve ilginizi çekebileceğini düşündük. 
Bir kaçamak planlamak ve sadece yemek ödemeli ücretsiz otel konaklamalarının keyfini çıkarmak için harika bir zaman. 
Tekrar göz atmak isterseniz veya yenilikler hakkında sorularınız varsa, {support_email} adresine yanıt verin — mükemmel konaklamayı bulmanıza yardımcı olmaktan memnuniyet duyarız.

Sizi yakında tekrar ağırlamayı umuyoruz!

Saygılarımızla,
Freestays Destek Ekibi
{support_email}
{website_url}"""
    }
}


async def seed_email_templates(db):
    """Seed the email templates into the database for all languages"""
    # Build the update data for all languages
    update_data = {}
    
    for lang, templates in AFTERSALE_EMAIL_TEMPLATES.items():
        if lang == "en":
            # English is the default, store without language prefix
            update_data["aftersale_email_no_payment"] = templates["no_payment"]
            update_data["aftersale_email_stop_payment"] = templates["stop_payment"]
            update_data["aftersale_email_not_interested"] = templates["not_interested"]
            update_data["aftersale_email_new_offers"] = templates["new_offers"]
        else:
            # Other languages use language prefix
            update_data[f"aftersale_email_no_payment_{lang}"] = templates["no_payment"]
            update_data[f"aftersale_email_stop_payment_{lang}"] = templates["stop_payment"]
            update_data[f"aftersale_email_not_interested_{lang}"] = templates["not_interested"]
            update_data[f"aftersale_email_new_offers_{lang}"] = templates["new_offers"]
    
    result = await db.settings.update_one(
        {"type": "app_settings"},
        {"$set": update_data},
        upsert=True
    )
    
    return {
        "matched": result.matched_count,
        "modified": result.modified_count,
        "upserted": result.upserted_id is not None,
        "languages_seeded": list(AFTERSALE_EMAIL_TEMPLATES.keys())
    }


import bcrypt
import uuid
from datetime import datetime, timezone

# Default Admin Users
DEFAULT_ADMIN_USERS = [
    {
        "email": "rob.ozinga@freestays.eu",
        "name": "Rob Ozinga",
        "password": "Barneveld2026!@"
    },
    {
        "email": "ayhanekici@gmail.com",
        "name": "Ayhan Ekici",
        "password": "Barneveld2026!@"
    }
]

def hash_password(password: str) -> str:
    """Hash a password using bcrypt"""
    salt = bcrypt.gensalt()
    hashed = bcrypt.hashpw(password.encode('utf-8'), salt)
    return hashed.decode('utf-8')


async def seed_admin_users(db):
    """Seed default admin users into the database"""
    results = []
    
    for admin_data in DEFAULT_ADMIN_USERS:
        email = admin_data["email"]
        
        # Check if user already exists
        existing = await db.users.find_one({"email": email})
        
        if existing:
            # Update to ensure admin status
            await db.users.update_one(
                {"email": email},
                {"$set": {"is_admin": True, "role": "admin", "email_verified": True}}
            )
            results.append({"email": email, "action": "updated_to_admin"})
        else:
            # Create new admin user
            user_doc = {
                "user_id": f"user_{uuid.uuid4().hex[:12]}",
                "email": email,
                "name": admin_data["name"],
                "password": hash_password(admin_data["password"]),
                "is_admin": True,
                "role": "admin",
                "pass_code": f"ADMIN-{uuid.uuid4().hex[:8].upper()}",
                "pass_type": "annual",
                "email_verified": True,
                "referral_code": f"FS{uuid.uuid4().hex[:8].upper()}",
                "referral_count": 0,
                "created_at": datetime.now(timezone.utc).isoformat()
            }
            await db.users.insert_one(user_doc)
            results.append({"email": email, "action": "created"})
    
    return {"admins": results}


async def seed_all_defaults(db):
    """Seed all default settings into the database"""
    # Email templates for all languages
    email_result = await seed_email_templates(db)
    
    # Admin users
    admin_result = await seed_admin_users(db)
    
    return {
        "email_templates": email_result,
        "admin_users": admin_result,
        "success": True
    }
