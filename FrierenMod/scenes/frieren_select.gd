extends Control


func _ready() -> void:
	# 等原版完成本次窗口变化的容器调整，再按最终变换布局。
	get_viewport().size_changed.connect(_fit_to_viewport, CONNECT_DEFERRED)
	get_parent().item_rect_changed.connect(_fit_to_viewport, CONNECT_DEFERRED)
	_fit_to_viewport()


func _fit_to_viewport() -> void:
	# 原版 AnimatedBg 为分层动画放大并偏移；静态全幅图按实际视口布局。
	# 抵消父级缩放，不改变绘制层级，角色说明和按钮仍在背景上方。
	var parent_scale: Vector2 = get_parent().get_global_transform().get_scale()
	scale = Vector2.ONE / parent_scale
	size = get_viewport_rect().size
	global_position = Vector2.ZERO
