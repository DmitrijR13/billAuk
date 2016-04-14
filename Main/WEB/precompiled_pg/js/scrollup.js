jQuery(document).ready(function () {
    jQuery('#scrollup img').mouseover(function () {
        jQuery(this).animate({ opacity: 0.65 }, 100);
    }).mouseout(function () {
        jQuery(this).animate({ opacity: 1 }, 100);
    }).click(function () {
        $("html, body").animate({ scrollTop: 0 }, '500', 'swing');
        return false;
    });
    jQuery(window).scroll(function () {
        if (jQuery(document).scrollTop() > 0) {
            jQuery('#scrollup').fadeIn('fast');
        } else {
            jQuery('#scrollup').fadeOut('fast');
        }
    });
});
