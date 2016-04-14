if (history && history.navigationMode) history.navigationMode = 'compatible';
var formdata_original = true;
jQuery.fn.extend({
	FormNavigate: function(o){
	    formdata_original = jQuery('#'+statusHiddenId).attr("value") != "true";
		jQuery(window).bind('beforeunload', function (){
			if (!formdata_original) return settings.message;
		});

		var def = {
			message: '',
			aOutConfirm: 'a:not([target!=_blank])'
		};
		var settings = jQuery.extend(false, def, o);

		if (o.aOutConfirm && o.aOutConfirm != def.aOutConfirm){
			jQuery('a').addClass('aOutConfirmPlugin');
			jQuery(settings.aOutConfirm).removeClass("aOutConfirmPlugin");
			jQuery(settings.aOutConfirm).click(function(){
				formdata_original = true;
				return true;
			});
		}

		jQuery("a.aOutConfirmPlugin").unbind("click").click(function(){
			if (formdata_original == false)
				if(confirm(settings.message))
				{
					formdata_original = true;
			    }
			return formdata_original;
		});

		jQuery(this).delegate("input[type=text]:not(.ignore), textarea:not(.ignore), input[type='password']:not(.ignore), input[type='radio']:not(.ignore),  input[type='checkbox']:parent(span:not(.ignore)), input[type='checkbox']:not(.ignored):parent(*:not(span)),  input[type='file']:not(.ignore), select:not(.ignore)", "change keypress", function (event) {
		    formdata_original = false;
			jQuery('#'+statusPanelId).show("slow");
			jQuery('#'+statusHiddenId).attr("value", "true");
		});

		jQuery(this).find(":submit, input[type='image']").click(function(){
			formdata_original = true;
		});
	}
});