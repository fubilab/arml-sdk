exports.transform = function (model) {
  model.yamlmime = 'Media'
  model._disableActionbar = true

  console.log('[media]')

  return model
}
