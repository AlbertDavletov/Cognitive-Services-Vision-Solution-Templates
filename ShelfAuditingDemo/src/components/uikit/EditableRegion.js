import React, { Component } from 'react';
import { View, Text, PanResponder, StyleSheet } from 'react-native';

const CircleSize = 24;

const PointPosition = {
    TopLeft: 'top-left',
    TopRight: 'top-right',
    BottomRight: 'bottom-right',
    BottomLeft: 'bottom-left'
}

class EditableRegion extends React.Component {
    constructor(props) {
        super(props);
        const { position } = props;

        this._originalLeft = position.left;
        this._originalTop = position.top;
        this._originalWidth = position.width;
        this._originalHeight = position.height;

        this.state = {
            left: position.left,
            top: position.top,
            width: position.width,
            height: position.height,
            topLeftPressed: false,
            topRightPressed: false,
            bottomRightPressed: false,
            bottomLeftPressed: false
        };

        this.topLeftPanResponder = this.getPanResponder(PointPosition.TopLeft);
        this.topRightPanResponder = this.getPanResponder(PointPosition.TopRight);
        this.bottomRightPanResponder = this.getPanResponder(PointPosition.BottomRight);
        this.bottomLeftPanResponder = this.getPanResponder(PointPosition.BottomLeft);
    }

    render() {
        const { data } = this.props;
        const { selectedRegion, selectedRegionLabelPanel, selectedRegionLabel, circleStyle } = styles;
    
        let component = 
            <View style={{
                position: 'absolute',
                left: this.state.left - CircleSize / 2, 
                top: this.state.top - CircleSize / 2,
                width: this.state.width + CircleSize,
                height: this.state.height + CircleSize,
                padding: CircleSize / 2,
            }}>   
                <View style={selectedRegion}>
                    <View style={selectedRegionLabelPanel}>
                        <Text numberOfLines={1} style={selectedRegionLabel}>{data.title}</Text>  
                    </View>
                </View>

                <View style={[circleStyle, { left: 0,  top: 0, backgroundColor:    this.state.topLeftPressed     ? 'gray' : 'white' }]} {...this.topLeftPanResponder.panHandlers}/>
                <View style={[circleStyle, { right: 0, top: 0, backgroundColor:    this.state.topRightPressed    ? 'gray' : 'white' }]} {...this.topRightPanResponder.panHandlers}/>
                <View style={[circleStyle, { right: 0, bottom: 0, backgroundColor: this.state.bottomRightPressed ? 'gray' : 'white' }]} {...this.bottomRightPanResponder.panHandlers}/>
                <View style={[circleStyle, { left: 0,  bottom: 0, backgroundColor: this.state.bottomLeftPressed  ? 'gray' : 'white' }]} {...this.bottomLeftPanResponder.panHandlers}/>
            </View>;
        return component;
    }

    _handlePanResponderMove = (event, gestureState, pointPosition) => {
        let xSign, ySign;

        switch (pointPosition) {
            case PointPosition.TopLeft:
                xSign = gestureState.dx < 0 ? 1 : -1;
                ySign = gestureState.dy < 0 ? 1 : -1;

                this.setState({
                    left: this._originalLeft + gestureState.dx,
                    top: this._originalTop + gestureState.dy,
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;

            case PointPosition.TopRight:
                xSign = gestureState.dx > 0 ? 1 : -1;
                ySign = gestureState.dy < 0 ? 1 : -1;

                this.setState({
                    top: this._originalTop + gestureState.dy,
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;

            case PointPosition.BottomRight:
                xSign = gestureState.dx > 0 ? 1 : -1;
                ySign = gestureState.dy > 0 ? 1 : -1;
        
                this.setState({
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;

            case PointPosition.BottomLeft:
                xSign = gestureState.dx < 0 ? 1 : -1;
                ySign = gestureState.dy > 0 ? 1 : -1;
        
                this.setState({
                    left: this._originalLeft + gestureState.dx,
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;
        }
    };

    _handlePandResponderGrant = (event, gestureState, pointPosition) => {
        if (this.props.positionChange) {
            const position = {
                left: this.state.left,
                top: this.state.top,
                width: this.state.width,
                height: this.state.height,
            }
            this.props.positionChange(true, position);
        }

        switch (pointPosition) {
            case PointPosition.TopLeft:
                this.setState({ topLeftPressed: true });
                break;

            case PointPosition.TopRight:
                this.setState({ topRightPressed: true });
                break;

            case PointPosition.BottomRight:
                this.setState({ bottomRightPressed: true });
                break;

            case PointPosition.BottomLeft:
                this.setState({ bottomLeftPressed: true });
                break;
        }
    }

    _handlePanResponderEnd = (event, gestureState, pointPosition) => {
        let xSign = 1;
        let ySign = 1;

        if (this.props.positionChange) {
            const position = {
                left: this.state.left,
                top: this.state.top,
                width: this.state.width,
                height: this.state.height,
            }
            this.props.positionChange(false, position);
        }

        switch (pointPosition) {
            case PointPosition.TopLeft:
                this.setState({ topLeftPressed: false });
                xSign = -1;
                ySign = -1;
                
                this._originalLeft += gestureState.dx;
                this._originalTop += gestureState.dy;
                break;

            case PointPosition.TopRight:
                this.setState({ topRightPressed: false });
                xSign = +1;
                ySign = -1;

                this._originalTop += gestureState.dy;
                break;

            case PointPosition.BottomRight:
                this.setState({ bottomRightPressed: false });
                xSign = +1;
                ySign = +1;
                break;

            case PointPosition.BottomLeft:
                this.setState({ bottomLeftPressed: false });
                xSign = -1;
                ySign = +1;

                this._originalLeft += gestureState.dx;
                break;
        }

        this._originalWidth += xSign * gestureState.dx;
        this._originalHeight += ySign * gestureState.dy;
    };

    getPanResponder(pointPosition) {
        return PanResponder.create({
            onStartShouldSetPanResponder: (event, gestureState) => { return true; },
            onMoveShouldSetPanResponder: (event, gestureState) => { return true; },
            onPanResponderGrant: (event, gestureState) => this._handlePandResponderGrant(event, gestureState, pointPosition),
            onPanResponderMove: (event, gestureState) => this._handlePanResponderMove(event, gestureState, pointPosition),
            onPanResponderRelease: (event, gestureState) => this._handlePanResponderEnd(event, gestureState, pointPosition),
            onPanResponderTerminate: (event, gestureState) => this._handlePanResponderEnd(event, gestureState, pointPosition)
        });
    }
}

const styles = StyleSheet.create({
    circleStyle: {
        position: 'absolute',
        width: CircleSize,
        height: CircleSize,
        backgroundColor: 'green',
        borderRadius: CircleSize / 2,
      },
    selectedRegion: {
        //position: 'absolute',
        flex: 1,
        borderWidth: 2,
        backgroundColor: 'transparent',
        borderRadius: 2,
        borderColor: 'white',
        borderStyle: 'dashed'
    },
    selectedRegionLabelPanel: {
        top: -42, 
        borderWidth: 0,
        margin: -2,
        borderRadius: 1,
        minWidth: 100,
        backgroundColor: 'white'
    },
    selectedRegionLabel: {
        color: 'black',
        padding: 4
    },
})

export { EditableRegion };