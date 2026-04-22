const stripe = Stripe('pk_test_51T8qXSAiTLgSn801gjaUC9jDcr77A1wpeBmQyAwDQKUPAetvkcPPfkK1zm1mtup08eZBQNd7hfyZlZKCSR2f0S6n001Ji5UpO6'); // Pega aquí tu clave publicable completa
const elements = stripe.elements();

const card = elements.create('card', {
    hidePostalCode: true,
    style: {
        base: {
            color: '#2F2F2F',
            fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
            fontSmoothing: 'antialiased',
            fontSize: '16px',
            '::placeholder': { color: '#aab7c4' }
        },
        invalid: {
            color: '#CD5C5C',
            iconColor: '#CD5C5C'
        }
    }
});

card.mount('#card-element');

const form = document.getElementById('payment-form');

form.addEventListener('submit', async (event) => {
    const metodoPago = document.querySelector('input[name="metodoPago"]:checked').value;

    if (metodoPago !== 'tarjeta') {
        return;
    }

    event.preventDefault();

    const btnConfirmar = document.getElementById('submit-button');
    btnConfirmar.disabled = true;

    const { paymentMethod, error } = await stripe.createPaymentMethod({
        type: 'card',
        card: card,
    });

    if (error) {
        const errorElement = document.getElementById('card-errors');
        errorElement.textContent = error.message;
        btnConfirmar.disabled = false;
    } else {
        document.getElementById('transactionId').value = paymentMethod.id;
        document.getElementById('ultimosDigitos').value = paymentMethod.card.last4;
        document.getElementById('estadoPago').value = "PENDIENTE"; 

        form.submit();
    }
});